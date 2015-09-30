/****************************************************************
Core header file for SparkFun ADC Edison Block Support

1 Jun 2015- Mike Hord, SparkFun Electronics
Code developed in Intel's Eclipse IOT-DK

This code requires the Intel mraa library to function; for more
information see https://github.com/intel-iot-devkit/mraa

This code is beerware; if you use it, please buy me (or any other
SparkFun employee) a cold beverage next time you run into one of
us at the local.
****************************************************************/

#ifndef __ads1015_h__
#define __ads1015_h__

#include "mraa.hpp"

// Defines for all the registers and bits. Not that many on this part.

#define CONVERSION 			0      		// Conversion result register.
#define CONFIG     			1      		// 16-bit configuration register.
#define THRESHL    			2      		// Low threshold setting. Not used (yet).
#define THRESHH    			3      		// High threshold setting. Not used (yet).

// Channel selection and read start stuff- the high nibble of the 16-bit cfg register controls the start of a single conversion, the channel(s) read,
//  and whether they're read single ended or differential.
#define CHANNEL_MASK 		0x3000 		// There are four channels, and single ended reads are specified by a two-bit address at bits 13:12
#define SINGLE_ENDED  		0x4000   	// Set for single-ended
#define START_READ   		0x8000 		// To start a read, we set the highest bit of the highest nibble.
#define CFG_REG_CHL_MASK 	0xf000 		// Used to clear the high nibble of the cfg reg before we start our read request.
#define BUSY_MASK     		0x8000 		// When the highest bit in the cfg reg is set, the conversion is done.
#define CHANNEL_SHIFT 		12  		// shift the raw channel # by this

// PGA settings and stuff. These are bits 11:9 of the cfg reg
#define RANGE_SHIFT 		9  			// how far to shift our prepared data to drop it into the right spot in the cfg reg
#define RANGE_MASK		 	0x0E00 		// bits to clear for gain parameter
enum VoltageRange : uint8_t { _6_144V = 0, _4_096V, _2_048V, _1_024V, _0_512V, _0_256V, VOLTAGE_MASK = 0x07};


class ads1015
{
  public:
    ads1015(mraa::I2c* myPort, unsigned short myI2CAddress): _myPort(myPort), _myI2CAddress(myI2CAddress)
	{
		_myPort->address(myI2CAddress); 
	}

    ~ads1015(){};
	
    // Returns the current reading on a channel, scaled by the current scaler and
	//  presented as a floating point number.
	float getResult(uint8_t channel)
	{
	  int16_t rawVal = getRawResult(channel);
	  return (float)rawVal * _scaler[channel]/1000;
	}
   
    
	// Single ended raw version. Single-ended results are effectively unsigned 11-bit
	//  values, from 0 to 2047.
	int16_t getRawResult(uint8_t channel)
	{
		uint16_t cfgRegVal = getConfigRegister();
		
		cfgRegVal &= ~CHANNEL_MASK; // clear existing channel settings
		cfgRegVal |= SINGLE_ENDED;  // set the SE bit for a s-e read
		cfgRegVal |= (channel<<CHANNEL_SHIFT) & CHANNEL_MASK; // put the channel bits in
		
		//add PGA for that channel !!!
		//cfgRegVal |= RANGE_MASK;
		//cfgRegVal |= (_range[channel] << RANGE_SHIFT) & RANGE_MASK;
		
		
		cfgRegVal |= START_READ;    // set the start read bit

		setConfigRegister(cfgRegVal);

		return readADC();
	}
    
	// Sets the voltage range extent. There are 6 settable ranges:
	//  _0_256V - Range is -0.256V to 0.255875V, and step size is 125uV.
	//  _0_512V - Range is -0.512V to 0.51175V, and step size is 250uV.
	//  _1_024V - Range is -1.024V to 1.0235V, and step size is 500uV.
	//  _2_048V - Range is -2.048V to 2.047V, and step size is 1mV.
	//  _4_096V - Range is -4.096V to 4.094V, and step size is 2mV.
	//  _6_144V - Range is -6.144V to 6.141V, and step size is 3mV.
	// The default setting is _2_048V.
	// NB!!! Just because FS reading is > 3.3V doesn't mean you can take an
	//  input above 3.3V! Keep your input voltages below 3.3V to avoid damage!
	void setRange(uint8_t channel, VoltageRange range)
	{
		uint16_t cfgRegVal = getConfigRegister();
		
		_range[channel] = range;
	  
		cfgRegVal &= ~RANGE_MASK;
		cfgRegVal |= (_range[channel] << RANGE_SHIFT) & RANGE_MASK;

		//add the channel!!!
		//cfgRegVal |= CHANNEL_MASK;
		//cfgRegVal |= (channel<<CHANNEL_SHIFT) & CHANNEL_MASK; // put the channel bits in
		
		
		setConfigRegister(cfgRegVal);
	  
		switch (range)
		{
		  case _6_144V:
			  _scaler[channel] = 3.0; // each count represents 3.0 mV
			  break;
		  case _4_096V:
			  _scaler[channel] = 2.0; // each count represents 2.0 mV
			  break;
		  case _2_048V:
			  _scaler[channel] = 1.0; // each count represents 1.0 mV
			  break;
		  case _1_024V:
			  _scaler[channel] = 0.5; // each count represents 0.5mV
			  break;
		  case _0_512V:
			  _scaler[channel] = 0.25; // each count represents 0.25mV
			  break;
		  case _0_256V:
			  _scaler[channel] = 0.125; // each count represents 0.125mV
			  break;
		  default:
			  _scaler[channel] = 1.0;  // here be dragons
			  break;
		}
	}
    
	// The _scaler variable holds a floating point value representing the number of
	//  millivolts per LSB. At its coarsest the ADS1015 is reporting 3mV per bit,
	//  and at its finest, 0.125mV (125uV).
	float getScaler(uint8_t channel)
	{
		return _scaler[channel];
	}
    
	// Config register read/write. I'm leaving these public against my better
	//  judgement, but be careful! You can really screw stuff up here.

	// Write some 16-bit value to the config register. See the datasheet for
	//  information about what these bits actually do.
	void setConfigRegister(uint16_t configValue)
	{
		uint8_t data[3];
		data[0] = 0x01;  // address of the config register
		data[1] = configValue>>8;
		data[2] = configValue;
		_myPort->write(data,3);
	}
	
   // Get the config register.
	uint16_t getConfigRegister()
	{
		uint16_t cfgRegVal = 0;
		_myPort->readBytesReg(CONFIG, (uint8_t*)&cfgRegVal, 2);
		cfgRegVal = (cfgRegVal>>8) | (cfgRegVal<<8);
		return cfgRegVal;
	}
	
  private:
    
	// This handles the actual read of the ADC- starting the conversion and waiting
	//  for it to complete. It returns an invalid number (-32768) if the ADC didn't
	//  respond in a timely manner (i.e., within 10ms, minimum). The highest
	//  sampling rate available is 3300 sps, so we expect to have an answer within
	//  300 us or so.
	int16_t readADC()
	{
		uint16_t cfgRegVal = getConfigRegister();
		cfgRegVal |= START_READ; // set the start read bit
		setConfigRegister(cfgRegVal);

		uint8_t result[2];
		int16_t fullValue=0;
		uint8_t busyDelay = 0;

		while ((getConfigRegister() & BUSY_MASK) == 0)
		{
		  usleep(100);
		  if(busyDelay++ > 100) return 0xffff;
		}

		_myPort->readBytesReg(CONVERSION, result, 2);

		fullValue = (result[0]<<8) + result[1];
		return int(fullValue>>4);
	}
    
	mraa::I2c* _myPort;
    uint8_t _myI2CAddress;
    float _scaler[4] = {1}; //scaler per channel
	VoltageRange _range[4] = {_2_048V};
};
#endif
