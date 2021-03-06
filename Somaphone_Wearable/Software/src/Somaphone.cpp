/*
 * Author: Antony Rayzhekov <antony@raijekov.cc>
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#include <libgen.h> 
#include <iostream>
#include <unistd.h> 
#include <string>
#include <chrono>
#include <thread>
#include <cmath>
#include <inttypes.h>
#include <signal.h>
#include "drivers/LSM9DS0.h"
#include "drivers/ADS1015.h"
#include "oscpack/osc/OscOutboundPacketStream.h"
#include "oscpack/ip/UdpSocket.h"

#define OUTPUT_BUFFER_SIZE		1024	//OSC pack buffer size

using namespace std;

volatile uint16_t pulse_bang 	= 0;
volatile uint16_t pulse_bpm 	= 0;
volatile float pulse_value_f 	= 0;
volatile float gsr_value_f 		= 0;
volatile float rst_value_f 		= 0;
volatile float battery_level	= 3.2;
const char* OSC_ADDRESS 		= "127.0.0.1";				//target IP
int OSC_PORT 					= 4444;						//target port
float sampleFreq				= 100.0f;					//sample frequency in Hz
string 							base_osc_addr = "edison";	//device name
bool calibrate 					= false;					//re-calibrate on start-up flag

//private
char buffer[OUTPUT_BUFFER_SIZE];
clock_t startTime;
float elapsed_secs 				= 0;
int counter 					= 0;
LSM9DS0 						*imu;
mraa::I2c						*adc_i2c;
bool shouldRun					= true;

// Define the function to be called when ctrl-c (SIGINT) signal is sent to process
void signal_callback_handler(int signum)
{
	printf("Terminating...\n");
	shouldRun = false;
}

void showhelp()
{
	printf("Options:\n");
	printf("\t--osc:\ttarget IP address\n");
	printf("\t--port:\ttarget port\n");
	printf("\t--rate:\tsampling rate in Hz\n");
	printf("\t--name:\tdevice name\n");
	printf("\t--calibrate:\tsensor re-calibration\n");
	printf("\t--help:\thelp text\n\n");
}

void process_args(int argc, char *argv[])
{
	printf("Somaphone v0.1a (cc) BY-SA 2015 Antoni Rayzhekov\n");
	
	int i = 0;
	while(i < argc)
	{
		if(strcmp(argv[i], "--osc") == 0)
		{
			i++;
			if(i<argc) OSC_ADDRESS = argv[i];
		}
		
		else if(strcmp(argv[i], "--port") == 0) 
		{
			i++;
			if(i<argc) OSC_PORT = atoi(argv[i]); 
		}
		
		else if(strcmp(argv[i], "--rate") == 0) 
		{
			i++;
			if(i<argc) sampleFreq = atof(argv[i]);
		}
		
		else if(strcmp(argv[i], "--name") == 0) 
		{
			i++;
			if(i<argc) base_osc_addr = argv[i];
		}
		
		else if(strcmp(argv[i], "--calibrate") == 0) 
		{
			calibrate = true;
		}
		
		else if(strcmp(argv[i], "--help") == 0) 
		{
			showhelp();
		}
		i++;
	}
	
}

//constructs osc address using basename and sub address: /devicename/gyro
const char* osc_addr(string basename, const char* subaddr)
{
	return basename.insert(0,"/").append("/").append(subaddr).c_str();
}

/******************************************************************************************************************
 * SOMAPHONE :: MAIN
 ******************************************************************************************************************/
int main(int argc, char *argv[])
{
	// Register signal and signal handler
	signal(SIGINT, signal_callback_handler);
	   
 /**********************************************************************************************************
 * SOMAPHONE :: Initializing
 **********************************************************************************************************/
	/*
	char resolved_path[1024]; 
    realpath(argv[0], resolved_path); 
	char* program_path = dirname(resolved_path);
    */
//Parse & process command line arguments
	process_args(argc, argv); // --osc 192.168.0.5 --port 4444 --hz 60 
	
	sleep(10); //???
//Init 9DOF
	imu = new LSM9DS0(0x6B, 0x1D);
	uint16_t imuResult = imu->begin(LSM9DS0::gyro_scale::G_SCALE_245DPS, 
									LSM9DS0::accel_scale::A_SCALE_2G, 
									LSM9DS0::mag_scale::M_SCALE_2GS, 
									LSM9DS0::gyro_odr::G_ODR_190_BW_125,
									LSM9DS0::accel_odr::A_ODR_200,
									LSM9DS0::mag_odr::M_ODR_125);
	imu->setAccelABW(LSM9DS0::accel_abw::A_ABW_50);
	cout << hex << "9DOF Chip ID:" << imuResult << endl;


//Init ADC
	adc_i2c = new mraa::I2c(1);
	ads1015 adc(adc_i2c, 0x48);
	// There are 6 settable ranges: 
	//  _0_256V - Range is -0.256V to 0.255875V, and step size is 125uV.
	//  _0_512V - Range is -0.512V to 0.51175V, and step size is 250uV.
	//  _1_024V - Range is -1.024V to 1.0235V, and step size is 500uV.
	//  _2_048V - Range is -2.048V to 2.047V, and step size is 1mV.
	//  _4_096V - Range is -4.096V to 4.094V, and step size is 2mV.
	//  _6_144V - Range is -6.144V to 6.141V, and step size is 3mV.
	// The default setting is _2_048V.
	// NB!!! Just because FS reading is > 3.3V doesn't mean you can take an
	//  input above 3.3V! Keep your input voltages below 3.3V to avoid damage!
	adc.setRange(0, _2_048V); //_2_048V or _4_096V 	<-- heart rate sensor
	adc.setRange(1, _2_048V); //_1_024V or _0_512V	<-- GSR sensor
	adc.setRange(2, _2_048V); //_4_096V				<-- btn1
	//adc.setRange(3, _4_096V); //_4_096V			<-- battery level

//Init OSC
	UdpTransmitSocket transmitSocket( IpEndpointName( OSC_ADDRESS, OSC_PORT ) );
	osc::OutboundPacketStream p( buffer, OUTPUT_BUFFER_SIZE );
	cout << "OSC " << OSC_ADDRESS  << ":" << OSC_PORT << " @ " << sampleFreq << "Hz" << endl;
	
//calibrate?
	if(calibrate) imu->recalibrate();

/**********************************************************************************************************
 * SOMAPHONE :: MAIN LOOP
 **********************************************************************************************************/
	while (shouldRun) {

		//start timing
		startTime = clock();

		counter++;
		
		//read 9DOF sensors
		imu->Read(sampleFreq);
		
		//read ADC
		pulse_value_f = adc.getResult(0);
		gsr_value_f = adc.getResult(1);
		rst_value_f = adc.getResult(2);
		//if((counter % 10) == 0) 
		battery_level = adc.getResult(3); //each 10sec 
		
		//construct the OSC bundle
		p << osc::BeginBundleImmediate
				<< osc::BeginMessage( osc_addr (base_osc_addr, "info") ) //use name
					<< 1			//ID
					<< (float)elapsed_secs	//read&process time
					<< (int)counter		//tick counter
					<< (float)rst_value_f	//RESET
					<< (float)imu->deltat
					<< (int)imu->overflow		//gyro, acc, mag data overflow
				<< osc::EndMessage

				<< osc::BeginMessage( osc_addr (base_osc_addr, "gsr" ) )
					<< (float) gsr_value_f	//Galvanic Skin Response
				<< osc::EndMessage

				<< osc::BeginMessage( osc_addr (base_osc_addr, "pulse" ) )
					<< (float) pulse_value_f	//Heart raw data
					<< (int)pulse_bang	//Heart Pulse Bang
					<< (float)pulse_bpm	//estimation of the heart rate in BPM
				<< osc::EndMessage

				<< osc::BeginMessage( osc_addr (base_osc_addr, "gyro" ) )
					<< (float)imu->gx_f
					<< (float)imu->gy_f
					<< (float)imu->gz_f
					
				<< osc::EndMessage
				
				<< osc::BeginMessage( osc_addr (base_osc_addr, "acc" ) )
					<< (float)imu->ax_f
					<< (float)imu->ay_f
					<< (float)imu->az_f
					
				<< osc::EndMessage
				
				<< osc::BeginMessage( osc_addr (base_osc_addr, "mag" ) )
					<< (float)imu->mx_f
					<< (float)imu->my_f
					<< (float)imu->mz_f
					
				<< osc::EndMessage
				
				<< osc::BeginMessage( osc_addr (base_osc_addr, "orientation" ) )
					<< (float)imu->pitch
					<< (float)imu->yaw
					<< (float)imu->roll
					
				<< osc::EndMessage
				
				<< osc::BeginMessage( osc_addr (base_osc_addr, "quaternion" ) )
					<< (float)imu->q0
					<< (float)imu->q1
					<< (float)imu->q2
					<< (float)imu->q3
				<< osc::EndMessage
				
		 << osc::EndBundle;

		//send the OSC bundle
		transmitSocket.Send( p.Data(), p.Size() );
		
		//clock the processing time and subtract it from the cycle period or better use a timer?
		clock_t endTime = clock();
		elapsed_secs = float(endTime - startTime) / CLOCKS_PER_SEC;
		int elapsed_usecs = (int)elapsed_secs*1000000;

		//wait until the next cycle
		usleep( ( (int) 1000000 / sampleFreq)); //- elapsed_usecs
		
		//clear the OSC bundle
		p.Clear();
	}

	return MRAA_SUCCESS;
}
