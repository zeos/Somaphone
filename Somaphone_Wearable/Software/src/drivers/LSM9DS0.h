/******************************************************************************
SFE_LSM9DS0.h
SFE_LSM9DS0 Library Header File
Jim Lindblom @ SparkFun Electronics
Original Creation Date: February 14, 2014 (Happy Valentines Day!)
Modified 14 Jul 2015 by Mike Hord to add Edison support
https://github.com/sparkfun/SparkFun_9DOF_Block_for_Edison_CPP_Library

This file prototypes the LSM9DS0 class, implemented in SFE_LSM9DS0.cpp. In
addition, it defines every register in the LSM9DS0 (both the Gyro and Accel/
Magnetometer registers).

** Supports only I2C connection! **

Development environment specifics:
  Code developed in Intel's Eclipse IOT-DK
  This code requires the Intel mraa library to function; for more
  information see https://github.com/intel-iot-devkit/mraa

This code is beerware; if you see me (or any other SparkFun employee) at the
local, and you've found our code helpful, please buy us a round!

Distributed as-is; no warranty is given.
******************************************************************************/
#ifndef __SFE_LSM9DS0_H__
#define __SFE_LSM9DS0_H__

#include <stdint.h>
#include "mraa.hpp"
#include <cmath>
#include <chrono>

using namespace std::chrono;

////////////////////////////
// LSM9DS0 Gyro Registers //
////////////////////////////
#define WHO_AM_I_G			0x0F
#define CTRL_REG1_G			0x20
#define CTRL_REG2_G			0x21
#define CTRL_REG3_G			0x22
#define CTRL_REG4_G			0x23
#define CTRL_REG5_G			0x24
#define REFERENCE_G			0x25
#define STATUS_REG_G		0x27
#define OUT_X_L_G			0x28
#define OUT_X_H_G			0x29
#define OUT_Y_L_G			0x2A
#define OUT_Y_H_G			0x2B
#define OUT_Z_L_G			0x2C
#define OUT_Z_H_G			0x2D
#define FIFO_CTRL_REG_G		0x2E
#define FIFO_SRC_REG_G		0x2F
#define INT1_CFG_G			0x30
#define INT1_SRC_G			0x31
#define INT1_THS_XH_G		0x32
#define INT1_THS_XL_G		0x33
#define INT1_THS_YH_G		0x34
#define INT1_THS_YL_G		0x35
#define INT1_THS_ZH_G		0x36
#define INT1_THS_ZL_G		0x37
#define INT1_DURATION_G		0x38

//////////////////////////////////////////
// LSM9DS0 Accel/Magneto (XM) Registers //
//////////////////////////////////////////
#define OUT_TEMP_L_XM		0x05
#define OUT_TEMP_H_XM		0x06
#define STATUS_REG_M		0x07
#define OUT_X_L_M			0x08
#define OUT_X_H_M			0x09
#define OUT_Y_L_M			0x0A
#define OUT_Y_H_M			0x0B
#define OUT_Z_L_M			0x0C
#define OUT_Z_H_M			0x0D
#define WHO_AM_I_XM			0x0F
#define INT_CTRL_REG_M		0x12
#define INT_SRC_REG_M		0x13
#define INT_THS_L_M			0x14
#define INT_THS_H_M			0x15
#define OFFSET_X_L_M		0x16
#define OFFSET_X_H_M		0x17
#define OFFSET_Y_L_M		0x18
#define OFFSET_Y_H_M		0x19
#define OFFSET_Z_L_M		0x1A
#define OFFSET_Z_H_M		0x1B
#define REFERENCE_X			0x1C
#define REFERENCE_Y			0x1D
#define REFERENCE_Z			0x1E
#define CTRL_REG0_XM		0x1F
#define CTRL_REG1_XM		0x20
#define CTRL_REG2_XM		0x21
#define CTRL_REG3_XM		0x22
#define CTRL_REG4_XM		0x23
#define CTRL_REG5_XM		0x24
#define CTRL_REG6_XM		0x25
#define CTRL_REG7_XM		0x26
#define STATUS_REG_A		0x27
#define OUT_X_L_A			0x28
#define OUT_X_H_A			0x29
#define OUT_Y_L_A			0x2A
#define OUT_Y_H_A			0x2B
#define OUT_Z_L_A			0x2C
#define OUT_Z_H_A			0x2D
#define FIFO_CTRL_REG		0x2E
#define FIFO_SRC_REG		0x2F
#define INT_GEN_1_REG		0x30
#define INT_GEN_1_SRC		0x31
#define INT_GEN_1_THS		0x32
#define INT_GEN_1_DURATION	0x33
#define INT_GEN_2_REG		0x34
#define INT_GEN_2_SRC		0x35
#define INT_GEN_2_THS		0x36
#define INT_GEN_2_DURATION	0x37
#define CLICK_CFG			0x38
#define CLICK_SRC			0x39
#define CLICK_THS			0x3A
#define TIME_LIMIT			0x3B
#define TIME_LATENCY		0x3C
#define TIME_WINDOW			0x3D
#define ACT_THS				0x3E
#define ACT_DUR				0x3F
  
  // global constants for 9 DoF fusion and AHRS (Attitude and Heading Reference System)
#define GyroMeasError M_PI * (40.0f / 180.0f)       // gyroscope measurement error in rads/s (shown as 3 deg/s)
#define GyroMeasDrift M_PI * (0.0f / 180.0f)      // gyroscope measurement drift in rad/s/s (shown as 0.0 deg/s/s)
// There is a tradeoff in the beta parameter between accuracy and response speed.
// In the original Madgwick study, beta of 0.041 (corresponding to GyroMeasError of 2.7 degrees/s) was found to give optimal accuracy.
// However, with this value, the LSM9SD0 response time is about 10 seconds to a stable initial quaternion.
// Subsequent changes also require a longish lag time to a stable output, not fast enough for a quadcopter or robot car!
// By increasing beta (GyroMeasError) by about a factor of fifteen, the response time constant is reduced to ~2 sec
// I haven't noticed any reduction in solution accuracy. This is essentially the I coefficient in a PID control sense; 
// the bigger the feedback coefficient, the faster the solution converges, usually at the expense of accuracy. 
// In any case, this is the free parameter in the Madgwick filtering and fusion scheme.
//#define beta sqrt(3.0f / 4.0f) * GyroMeasError   // compute beta
#define zeta 		sqrt(3.0f / 4.0f) * GyroMeasDrift   // compute zeta, the other free parameter in the Madgwick scheme usually set to a small or zero value
#define Kp 			2.0f * 5.0f // these are the free parameters in the Mahony filter and fusion scheme, Kp for proportional feedback, Ki for integral
#define Ki 			0.0f
#define betaDef		0.1f		// 2 * proportional gain

class LSM9DS0
{
public:
	
	// gyro_scale defines the possible full-scale ranges of the gyroscope:
	enum gyro_scale
	{
		G_SCALE_245DPS,		// 00:  245 degrees per second
		G_SCALE_500DPS,		// 01:  500 dps
		G_SCALE_2000DPS,	// 10:  2000 dps
	};
	// accel_scale defines all possible FSR's of the accelerometer:
	enum accel_scale
	{
		A_SCALE_2G,	// 000:  2g
		A_SCALE_4G,	// 001:  4g
		A_SCALE_6G,	// 010:  6g
		A_SCALE_8G,	// 011:  8g
		A_SCALE_16G	// 100:  16g
	};
	// mag_scale defines all possible FSR's of the magnetometer:
	enum mag_scale
	{
		M_SCALE_2GS,	// 00:  2Gs
		M_SCALE_4GS, 	// 01:  4Gs
		M_SCALE_8GS,	// 10:  8Gs
		M_SCALE_12GS,	// 11:  12Gs
	};
	// gyro_odr defines all possible data rate/bandwidth combos of the gyro:
	enum gyro_odr
	{							// ODR (Hz) --- Cutoff
		G_ODR_95_BW_125  = 0x0, //   95         12.5
		G_ODR_95_BW_25   = 0x1, //   95          25
		// 0x2 and 0x3 define the same data rate and bandwidth
		G_ODR_190_BW_125 = 0x4, //   190        12.5
		G_ODR_190_BW_25  = 0x5, //   190         25
		G_ODR_190_BW_50  = 0x6, //   190         50
		G_ODR_190_BW_70  = 0x7, //   190         70
		G_ODR_380_BW_20  = 0x8, //   380         20
		G_ODR_380_BW_25  = 0x9, //   380         25
		G_ODR_380_BW_50  = 0xA, //   380         50
		G_ODR_380_BW_100 = 0xB, //   380         100
		G_ODR_760_BW_30  = 0xC, //   760         30
		G_ODR_760_BW_35  = 0xD, //   760         35
		G_ODR_760_BW_50  = 0xE, //   760         50
		G_ODR_760_BW_100 = 0xF, //   760         100
	};
	// accel_oder defines all possible output data rates of the accelerometer:
	enum accel_odr
	{
		A_POWER_DOWN, 	// Power-down mode (0x0)
		A_ODR_3125,		// 3.125 Hz	(0x1)
		A_ODR_625,		// 6.25 Hz (0x2)
		A_ODR_125,		// 12.5 Hz (0x3)
		A_ODR_25,		// 25 Hz (0x4)
		A_ODR_50,		// 50 Hz (0x5)
		A_ODR_100,		// 100 Hz (0x6)
		A_ODR_200,		// 200 Hz (0x7)
		A_ODR_400,		// 400 Hz (0x8)
		A_ODR_800,		// 800 Hz (9)
		A_ODR_1600		// 1600 Hz (0xA)
	};

      // accel_abw defines all possible anti-aliasing filter rates of the accelerometer:
	enum accel_abw
	{
		A_ABW_773,		// 773 Hz (0x0)
		A_ABW_194,		// 194 Hz (0x1)
		A_ABW_362,		// 362 Hz (0x2)
		A_ABW_50,		//  50 Hz (0x3)
	};


	// mag_oder defines all possible output data rates of the magnetometer:
	enum mag_odr
	{
		M_ODR_3125,	// 3.125 Hz (0x00)
		M_ODR_625,	// 6.25 Hz (0x01)
		M_ODR_125,	// 12.5 Hz (0x02)
		M_ODR_25,	// 25 Hz (0x03)
		M_ODR_50,	// 50 (0x04)
		M_ODR_100,	// 100 Hz (0x05)
	};

	// We'll store the gyro, accel, and magnetometer readings in a series of
	// public class variables. Each sensor gets three variables -- one for each
	// axis. Call readGyro(), readAccel(), and readMag() first, before using
	// these variables!
	// These values are the RAW signed 16-bit readings from the sensors.
	int16_t ax_i, ay_i, az_i; // x, y, and z axis readings of the accelerometer
	int16_t gx_i, gy_i, gz_i; // x, y, and z axis readings of the gyroscope
	int16_t mx_i, my_i, mz_i; // x, y, and z axis readings of the magnetometer
	int16_t temperature_i;
	
	volatile float ax_f, ay_f, az_f;
	volatile float gx_f, gy_f, gz_f;
	volatile float mx_f, my_f, mz_f;
	volatile float temperature_f;
	
	bool newAccelData 				= false;
	bool newMagData 				= false;
	bool newGyroData 				= false;
	bool overflow 					= false;
	
	float pitch, yaw, roll, heading;
	float deltat = 0.08f;        // integration interval for both filter schemes
	
	high_resolution_clock::time_point lastUpdate = high_resolution_clock::now();    // used to calculate integration interval
	high_resolution_clock::time_point Now = high_resolution_clock::now();;           // used to calculate integration interval
	
	float abias[3] = {0, 0, 0}, gbias[3] = {0, 0, 0};
	
	float q[4] = {1.0f, 0.0f, 0.0f, 0.0f}; // vector to hold quaternion
	float eInt[3] = {0.0f, 0.0f, 0.0f}; // vector to hold integral error for Mahony method
//x-io
	volatile float beta = betaDef;								// 2 * proportional gain (Kp)
	volatile float q0 = 1.0f, q1 = 0.0f, q2 = 0.0f, q3 = 0.0f;	// quaternion of sensor frame relative to auxiliary frame

	// LSM9DS0 -- LSM9DS0 class constructor
	// The constructor will set up a handful of private variables, and set the
	// communication mode as well.
	// Input:
	//	- gAddr = I2C address of the gyroscope.
	//	- xmAddr = I2C address of the accel/mag.
	LSM9DS0(uint8_t gAddr, uint8_t xmAddr):
		ax_i(0), ay_i(0), az_i(0),
		gx_i(0), gy_i(0), gz_i(0),
		mx_i(0), my_i(0), mz_i(0),
	  	temperature_i(0),
		
		ax_f(0), ay_f(0), az_f(0),
		gx_f(0), gy_f(0), gz_f(0),
		mx_f(0), my_f(0), mz_f(0),
		temperature_f(0),
		
		gScale(G_SCALE_245DPS), 
		aScale(A_SCALE_2G), 
		mScale(M_SCALE_2GS),
		
		gRes(0), aRes(0), mRes(0)
	{
	  gyro = new mraa::I2c(1);
	  gyro->address(gAddr);
	  
	  xm = new mraa::I2c(1);
	  xm->address(xmAddr);
	}
	
	// begin() -- Initialize the gyro, accelerometer, and magnetometer.
	// This will set up the scale and output rate of each sensor. It'll also
	// "turn on" every sensor and every axis of every sensor.
	// Input:
	//	- gScl = The scale of the gyroscope. This should be a gyro_scale value.
	//	- aScl = The scale of the accelerometer. Should be a accel_scale value.
	//	- mScl = The scale of the magnetometer. Should be a mag_scale value.
	//	- gODR = Output data rate of the gyroscope. gyro_odr value.
	//	- aODR = Output data rate of the accelerometer. accel_odr value.
	//	- mODR = Output data rate of the magnetometer. mag_odr value.
	// Output: The function will return an unsigned 16-bit value. The most-sig
	//		bytes of the output are the WHO_AM_I reading of the accel. The
	//		least significant two bytes are the WHO_AM_I reading of the gyro.
	// All parameters have a defaulted value, so you can call just "begin()".
	// Default values are FSR's of:  245DPS, 2g, 2Gs; ODRs of 95 Hz for 
	// gyro, 100 Hz for accelerometer, 100 Hz for magnetometer.
	// Use the return value of this function to verify communication.
	uint16_t begin(gyro_scale gScl = G_SCALE_245DPS, 
				accel_scale aScl = A_SCALE_2G, mag_scale mScl = M_SCALE_2GS,
				gyro_odr gODR = G_ODR_95_BW_125, accel_odr aODR = A_ODR_50, 
				mag_odr mODR = M_ODR_50)
	{
		// Store the given scales in class variables. These scale variables
		// are used throughout to calculate the actual g's, DPS,and Gs's.
		gScale = gScl;
		aScale = aScl;
		mScale = mScl;
		
		// Once we have the scale values, we can calculate the resolution
		// of each sensor. That's what these functions are for. One for each sensor
		calcgRes(); // Calculate DPS / ADC tick, stored in gRes variable
		calcmRes(); // Calculate Gs / ADC tick, stored in mRes variable
		calcaRes(); // Calculate g / ADC tick, stored in aRes variable
		
		// To verify communication, we can read from the WHO_AM_I register of
		// each device. Store those in a variable so we can return them.
		uint8_t gTest = gReadByte(WHO_AM_I_G);		// Read the gyro WHO_AM_I
		uint8_t xmTest = xmReadByte(WHO_AM_I_XM);	// Read the accel/mag WHO_AM_I
		
		// Gyro initialization stuff:
		initGyro();	// This will "turn on" the gyro. Setting up interrupts, etc.
		setGyroODR(gODR); // Set the gyro output data rate and bandwidth.
		setGyroScale(gScale); // Set the gyro range
		
		// Accelerometer initialization stuff:
		initAccel(); // "Turn on" all axes of the accel. Set up interrupts, etc.
		setAccelODR(aODR); // Set the accel data rate.
		setAccelScale(aScale); // Set the accel range.
		
		// Magnetometer initialization stuff:
		initMag(); // "Turn on" all axes of the mag. Set up interrupts, etc.
		setMagODR(mODR); // Set the magnetometer output data rate.
		setMagScale(mScale); // Set the magnetometer's range.
		
		// Once everything is initialized, return the WHO_AM_I registers we read:
		return (xmTest << 8) | gTest;
	}
	
	// readGyro() -- Read the gyroscope output registers.
	// This function will read all six gyroscope output registers.
	// The readings are stored in the class' gx, gy, and gz variables. Read
	// those _after_ calling readGyro().
	void readGyro()
	{
		uint8_t temp[6]; // We'll read six bytes from the gyro into temp
		gReadBytes(OUT_X_L_G, temp, 6); // Read 6 bytes, beginning at OUT_X_L_G
		gx_i = (temp[1] << 8) | temp[0]; // Store x-axis values into gx
		gy_i = (temp[3] << 8) | temp[2]; // Store y-axis values into gy
		gz_i = (temp[5] << 8) | temp[4]; // Store z-axis values into gz
	}
	
	// readAccel() -- Read the accelerometer output registers.
	// This function will read all six accelerometer output registers.
	// The readings are stored in the class' ax, ay, and az variables. Read
	// those _after_ calling readAccel().
	void readAccel()
	{
		uint8_t temp[6]; // We'll read six bytes from the accelerometer into temp	
		xmReadBytes(OUT_X_L_A, temp, 6); // Read 6 bytes, beginning at OUT_X_L_A
		ax_i = (temp[1] << 8) | temp[0]; // Store x-axis values into ax
		ay_i = (temp[3] << 8) | temp[2]; // Store y-axis values into ay
		az_i = (temp[5] << 8) | temp[4]; // Store z-axis values into az
	}
	
	// readMag() -- Read the magnetometer output registers.
	// This function will read all six magnetometer output registers.
	// The readings are stored in the class' mx, my, and mz variables. Read
	// those _after_ calling readMag().
	void readMag()
	{
		uint8_t temp[6]; // We'll read six bytes from the mag into temp	
		xmReadBytes(OUT_X_L_M, temp, 6); // Read 6 bytes, beginning at OUT_X_L_M
		mx_i = (temp[1] << 8) | temp[0]; // Store x-axis values into mx
		my_i = (temp[3] << 8) | temp[2]; // Store y-axis values into my
		mz_i = (temp[5] << 8) | temp[4]; // Store z-axis values into mz
	}

	// readTemp() -- Read the temperature output register.
	// This function will read two temperature output registers.
	// The combined readings are stored in the class' temperature variables. 
	// Read those _after_ calling readTemp().
	void readTemp()
	{
		uint8_t temp[2]; // We'll read two bytes from the temperature sensor into temp	
		xmReadBytes(OUT_TEMP_L_XM, temp, 2); // Read 2 bytes, beginning at OUT_TEMP_L_M
		temperature_i =  int16_t(temp[0]) + (int16_t(temp[1])<<8) ; // Temperature is a 12-bit signed integer
	}
	
	// calcGyro() -- Convert from RAW signed 16-bit value to degrees per second
	// This function reads in a signed 16-bit value and returns the scaled
	// DPS. This function relies on gScale and gRes being correct.
	// Input:
	//	- gyro = A signed 16-bit raw reading from the gyroscope.
	float calcGyro(int16_t gyro)
	{
		// Return the gyro raw reading times our pre-calculated DPS / (ADC tick):
		return gRes * gyro; 
	}
	
	// calcAccel() -- Convert from RAW signed 16-bit value to gravity (g's).
	// This function reads in a signed 16-bit value and returns the scaled
	// g's. This function relies on aScale and aRes being correct.
	// Input:
	//	- accel = A signed 16-bit raw reading from the accelerometer.
	float calcAccel(int16_t accel)
	{
		// Return the accel raw reading times our pre-calculated g's / (ADC tick):
		return aRes * accel;
	}
	
	// calcMag() -- Convert from RAW signed 16-bit value to Gauss (Gs)
	// This function reads in a signed 16-bit value and returns the scaled
	// Gs. This function relies on mScale and mRes being correct.
	// Input:
	//	- mag = A signed 16-bit raw reading from the magnetometer.
	float calcMag(int16_t mag)
	{
		// Return the mag raw reading times our pre-calculated Gs / (ADC tick):
		return mRes * mag;
	}
	
	// setGyroScale() -- Set the full-scale range of the gyroscope.
	// This function can be called to set the scale of the gyroscope to 
	// 245, 500, or 200 degrees per second.
	// Input:
	// 	- gScl = The desired gyroscope scale. Must be one of three possible
	//		values from the gyro_scale enum.
	void setGyroScale(gyro_scale gScl)
	{
		// We need to preserve the other bytes in CTRL_REG4_G. So, first read it:
		uint8_t temp = gReadByte(CTRL_REG4_G);
		// Then mask out the gyro scale bits:
		temp &= 0xFF^(0x3 << 4);
		// Then shift in our new scale bits:
		temp |= gScl << 4;
		// And write the new register value back into CTRL_REG4_G:
		gWriteByte(CTRL_REG4_G, temp);
		
		// We've updated the sensor, but we also need to update our class variables
		// First update gScale:
		gScale = gScl;
		// Then calculate a new gRes, which relies on gScale being set correctly:
		calcgRes();
	}
	
	// setAccelScale() -- Set the full-scale range of the accelerometer.
	// This function can be called to set the scale of the accelerometer to
	// 2, 4, 6, 8, or 16 g's.
	// Input:
	// 	- aScl = The desired accelerometer scale. Must be one of five possible
	//		values from the accel_scale enum.
	void setAccelScale(accel_scale aScl)
	{
		// We need to preserve the other bytes in CTRL_REG2_XM. So, first read it:
		uint8_t temp = xmReadByte(CTRL_REG2_XM);
		// Then mask out the accel scale bits:
		temp &= 0xFF^(0x3 << 3);
		// Then shift in our new scale bits:
		temp |= aScl << 3;
		// And write the new register value back into CTRL_REG2_XM:
		xmWriteByte(CTRL_REG2_XM, temp);
		
		// We've updated the sensor, but we also need to update our class variables
		// First update aScale:
		aScale = aScl;
		// Then calculate a new aRes, which relies on aScale being set correctly:
		calcaRes();
	}
	
	// setMagScale() -- Set the full-scale range of the magnetometer.
	// This function can be called to set the scale of the magnetometer to
	// 2, 4, 8, or 12 Gs.
	// Input:
	// 	- mScl = The desired magnetometer scale. Must be one of four possible
	//		values from the mag_scale enum.
	void setMagScale(mag_scale mScl)
	{
		// We need to preserve the other bytes in CTRL_REG6_XM. So, first read it:
		uint8_t temp = xmReadByte(CTRL_REG6_XM);
		// Then mask out the mag scale bits:
		temp &= 0xFF^(0x3 << 5);
		// Then shift in our new scale bits:
		temp |= mScl << 5;
		// And write the new register value back into CTRL_REG6_XM:
		xmWriteByte(CTRL_REG6_XM, temp);
		
		// We've updated the sensor, but we also need to update our class variables
		// First update mScale:
		mScale = mScl;
		// Then calculate a new mRes, which relies on mScale being set correctly:
		calcmRes();
	}
	
	// setGyroODR() -- Set the output data rate and bandwidth of the gyroscope
	// Input:
	//	- gRate = The desired output rate and cutoff frequency of the gyro.
	//		Must be a value from the gyro_odr enum (check above, there're 14).
	void setGyroODR(gyro_odr gRate)
	{
		// We need to preserve the other bytes in CTRL_REG1_G. So, first read it:
		uint8_t temp = gReadByte(CTRL_REG1_G);
		// Then mask out the gyro ODR bits:
		temp &= 0xFF^(0xF << 4);
		// Then shift in our new ODR bits:
		temp |= (gRate << 4);
		// And write the new register value back into CTRL_REG1_G:
		gWriteByte(CTRL_REG1_G, temp);
	}
	
	// setAccelODR() -- Set the output data rate of the accelerometer
	// Input:
	//	- aRate = The desired output rate of the accel.
	//		Must be a value from the accel_odr enum (check above, there're 11).
	void setAccelODR(accel_odr aRate)
	{
		// We need to preserve the other bytes in CTRL_REG1_XM. So, first read it:
		uint8_t temp = xmReadByte(CTRL_REG1_XM);
		// Then mask out the accel ODR bits:
		temp &= 0xFF^(0xF << 4);
		// Then shift in our new ODR bits:
		temp |= (aRate << 4);
		// And write the new register value back into CTRL_REG1_XM:
		xmWriteByte(CTRL_REG1_XM, temp);
	} 	

	// setAccelABW() -- Set the anti-aliasing filter rate of the accelerometer
	// Input:
	//	- abwRate = The desired anti-aliasing filter rate of the accel.
	//		Must be a value from the accel_abw enum (check above, there're 4).
	void setAccelABW(accel_abw abwRate)
	{
		// We need to preserve the other bytes in CTRL_REG2_XM. So, first read it:
		uint8_t temp = xmReadByte(CTRL_REG2_XM);
		// Then mask out the accel ABW bits:
		temp &= 0xFF^(0x3 << 6);
		// Then shift in our new ODR bits:
		temp |= (abwRate << 6);
		// And write the new register value back into CTRL_REG2_XM:
		xmWriteByte(CTRL_REG2_XM, temp);
	}

	// setMagODR() -- Set the output data rate of the magnetometer
	// Input:
	//	- mRate = The desired output rate of the mag.
	//		Must be a value from the mag_odr enum (check above, there're 6).
	void setMagODR(mag_odr mRate)
	{
		// We need to preserve the other bytes in CTRL_REG5_XM. So, first read it:
		uint8_t temp = xmReadByte(CTRL_REG5_XM);
		// Then mask out the mag ODR bits:
		temp &= 0xFF^(0x7 << 2);
		// Then shift in our new ODR bits:
		temp |= (mRate << 2);
		// And write the new register value back into CTRL_REG5_XM:
		xmWriteByte(CTRL_REG5_XM, temp);
	}

	// Since the Edison is not a real-time system, it's probably best to let the
	//  LSMDS0 set the pace of data collection, as sampling rate matters for some
	//  applications. These functions allow you to see if new data has been
	//  logged since the last read. Note that it does NOT check for overflow
	//  conditions!!!
	bool newXData()
	{
	  const uint8_t dReadyMask = 0b00001000;
	  uint8_t statusRegVal = xmReadByte(STATUS_REG_A);
	  if ((dReadyMask & statusRegVal) != 0)
	  {
		return true;
	  }
	  return false;
	}
	
	bool newMData()
	{
	  const uint8_t dReadyMask = 0b00001000;
	  uint8_t statusRegVal = xmReadByte(STATUS_REG_M);
	  if ((dReadyMask & statusRegVal) != 0)
	  {
		return true;
	  }
	  return false;
	}

	bool newGData()
	{
	  const uint8_t dReadyMask = 0b00001000;
	  uint8_t statusRegVal = gReadByte(STATUS_REG_G);
	  if ((dReadyMask & statusRegVal) != 0)
	  {
		return true;
	  }
	  return false;
	}
	
	// If you want to know whether an overflow has occurred, there are bits for
	//  that. These functions check for those conditions.
	bool xDataOverflow()
	{
	  const uint8_t dOverflowMask = 0b10000000;
	  uint8_t statusRegVal = xmReadByte(STATUS_REG_A);
	  if ((dOverflowMask & statusRegVal) != 0)
	  {
		return true;
	  }
	  return false;
	}
	
	bool gDataOverflow()
	{
	  const uint8_t dOverflowMask = 0b10000000;
	  uint8_t statusRegVal = xmReadByte(STATUS_REG_A);
	  if ((dOverflowMask & statusRegVal) != 0)
	  {
		return true;
	  }
	  return false;
	}
	
	bool mDataOverflow()
	{
	  const uint8_t dOverflowMask = 0b10000000;
	  uint8_t statusRegVal = xmReadByte(STATUS_REG_M);
	  if ((dOverflowMask & statusRegVal) != 0)
	  {
		return true;
	  }
	  return false;
	}
	
	bool recalibrate() //accumulate bias for 10s
	{
		return false;
	}

	void Read(float sampleFreq)
	{
		//wait until all data is available, this will block execution, is there a better way? 
		while ((newGyroData & newAccelData & newMagData) != true)
		{
		  if (newAccelData != true)
		  {
			newAccelData = newXData();
		  }
		  if (newGyroData != true)
		  {
			newGyroData = newGData();
		  }
		  if (newMagData != true)
		  {
			newMagData = newMData(); // Temp data is collected at the same rate as magnetometer data.
		  } 
		}

		newAccelData 	= false;
		newMagData 		= false;
		newGyroData 	= false;
		
		readGyro();           // Read raw gyro data
		gx_f = calcGyro(gx_i) - gbias[0];   // Convert to degrees per seconds, remove gyro biases
		gy_f = calcGyro(gy_i) - gbias[1];
		gz_f = calcGyro(gz_i) - gbias[2];
		
		readAccel();         // Read raw accelerometer data
		ax_f = calcAccel(ax_i) - abias[0];   // Convert to g's, remove accelerometer biases
		ay_f = calcAccel(ay_i) - abias[1];
		az_f = calcAccel(az_i) - abias[2];
		
		readMag();           	// Read raw magnetometer data
		mx_f = calcMag(mx_i);     // Convert to Gauss and correct for calibration
		my_f = calcMag(my_i);
		mz_f = calcMag(mz_i);
		
		readTemp();
		temperature_f = 21.0 + (float)temperature_i/8.; // slope is 8 LSB per degree C, just guessing at the intercept
		
		overflow = xDataOverflow() |  gDataOverflow() |   mDataOverflow();
		
		//calc orientation 
		Now = high_resolution_clock::now();
		deltat = duration_cast<microseconds>(Now - lastUpdate).count() / 1000000.0f;
		
		//MadgwickQuaternionUpdate(ax_f, ay_f, az_f, gx_f*M_PI/180.0f, gy_f*M_PI/180.0f, gz_f*M_PI/180.0f, mx_f, my_f, mz_f);
		MadgwickAHRSupdate(gx_f*M_PI/180.0f, gy_f*M_PI/180.0f, gz_f*M_PI/180.0f, ax_f, ay_f, az_f, mx_f, my_f, mz_f, deltat); //-> 1/sampleFreq
		
		CalcAngles(q0, q1, q2, q3, 3.58); //declination in Vienna = 3.58
		
		lastUpdate = high_resolution_clock::now();
	}
	
private:	

	mraa::I2c* 	gyro;
	mraa::I2c* 	xm;
	// gScale, aScale, and mScale store the current scale range for each 
	// sensor. Should be updated whenever that value changes.
	gyro_scale 	gScale;
	accel_scale aScale;
	mag_scale 	mScale;
	
	uint32_t micros();
	
	// gRes, aRes, and mRes store the current resolution for each sensor. 
	// Units of these values would be DPS (or g's or Gs's) per ADC tick.
	// This value is calculated as (sensor scale) / (2^15).
	float gRes, aRes, mRes;
	
	// initGyro() -- Sets up the gyroscope to begin reading.
	// This function steps through all five gyroscope control registers.
	// Upon exit, the following parameters will be set:
	//	- CTRL_REG1_G = 0x0F: Normal operation mode, all axes enabled. 
	//		95 Hz ODR, 12.5 Hz cutoff frequency.
	//	- CTRL_REG2_G = 0x00: HPF set to normal mode, cutoff frequency
	//		set to 7.2 Hz (depends on ODR).
	//	- CTRL_REG3_G = 0x88: Interrupt enabled on INT_G (set to push-pull and
	//		active high). Data-ready output enabled on DRDY_G.
	//	- CTRL_REG4_G = 0x00: Continuous update mode. Data LSB stored in lower
	//		address. Scale set to 245 DPS. SPI mode set to 4-wire.
	//	- CTRL_REG5_G = 0x00: FIFO disabled. HPF disabled.
	void initGyro()
	{
		/* CTRL_REG1_G sets output data rate, bandwidth, power-down and enables
		Bits[7:0]: DR1 DR0 BW1 BW0 PD Zen Xen Yen
		DR[1:0] - Output data rate selection
			00=95Hz, 01=190Hz, 10=380Hz, 11=760Hz
		BW[1:0] - Bandwidth selection (sets cutoff frequency)
			 Value depends on ODR. See datasheet table 21.
		PD - Power down enable (0=power down mode, 1=normal or sleep mode)
		Zen, Xen, Yen - Axis enable (o=disabled, 1=enabled)	*/
		gWriteByte(CTRL_REG1_G, 0x0F); // Normal mode, enable all axes
		
		/* CTRL_REG2_G sets up the HPF
		Bits[7:0]: 0 0 HPM1 HPM0 HPCF3 HPCF2 HPCF1 HPCF0
		HPM[1:0] - High pass filter mode selection
			00=normal (reset reading HP_RESET_FILTER, 01=ref signal for filtering,
			10=normal, 11=autoreset on interrupt
		HPCF[3:0] - High pass filter cutoff frequency
			Value depends on data rate. See datasheet table 26.
		*/
		gWriteByte(CTRL_REG2_G, 0x00); // Normal mode, high cutoff frequency
		
		/* CTRL_REG3_G sets up interrupt and DRDY_G pins
		Bits[7:0]: I1_IINT1 I1_BOOT H_LACTIVE PP_OD I2_DRDY I2_WTM I2_ORUN I2_EMPTY
		I1_INT1 - Interrupt enable on INT_G pin (0=disable, 1=enable)
		I1_BOOT - Boot status available on INT_G (0=disable, 1=enable)
		H_LACTIVE - Interrupt active configuration on INT_G (0:high, 1:low)
		PP_OD - Push-pull/open-drain (0=push-pull, 1=open-drain)
		I2_DRDY - Data ready on DRDY_G (0=disable, 1=enable)
		I2_WTM - FIFO watermark interrupt on DRDY_G (0=disable 1=enable)
		I2_ORUN - FIFO overrun interrupt on DRDY_G (0=disable 1=enable)
		I2_EMPTY - FIFO empty interrupt on DRDY_G (0=disable 1=enable) */
		// Int1 enabled (pp, active low), data read on DRDY_G:
		gWriteByte(CTRL_REG3_G, 0x88); 
		
		/* CTRL_REG4_G sets the scale, update mode
		Bits[7:0] - BDU BLE FS1 FS0 - ST1 ST0 SIM
		BDU - Block data update (0=continuous, 1=output not updated until read
		BLE - Big/little endian (0=data LSB @ lower address, 1=LSB @ higher add)
		FS[1:0] - Full-scale selection
			00=245dps, 01=500dps, 10=2000dps, 11=2000dps
		ST[1:0] - Self-test enable
			00=disabled, 01=st 0 (x+, y-, z-), 10=undefined, 11=st 1 (x-, y+, z+)
		SIM - SPI serial interface mode select
			0=4 wire, 1=3 wire */
		gWriteByte(CTRL_REG4_G, 0x00); // Set scale to 245 dps
		
		/* CTRL_REG5_G sets up the FIFO, HPF, and INT1
		Bits[7:0] - BOOT FIFO_EN - HPen INT1_Sel1 INT1_Sel0 Out_Sel1 Out_Sel0
		BOOT - Reboot memory content (0=normal, 1=reboot)
		FIFO_EN - FIFO enable (0=disable, 1=enable)
		HPen - HPF enable (0=disable, 1=enable)
		INT1_Sel[1:0] - Int 1 selection configuration
		Out_Sel[1:0] - Out selection configuration */
		gWriteByte(CTRL_REG5_G, 0x00);
		
	}
	
	// initAccel() -- Sets up the accelerometer to begin reading.
	// This function steps through all accelerometer related control registers.
	// Upon exit these registers will be set as:
	//	- CTRL_REG0_XM = 0x00: FIFO disabled. HPF bypassed. Normal mode.
	//	- CTRL_REG1_XM = 0x57: 100 Hz data rate. Continuous update.
	//		all axes enabled.
	//	- CTRL_REG2_XM = 0x00:  2g scale. 773 Hz anti-alias filter BW.
	//	- CTRL_REG3_XM = 0x04: Accel data ready signal on INT1_XM pin.
	void initAccel()
	{
		/* CTRL_REG0_XM (0x1F) (Default value: 0x00)
		Bits (7-0): BOOT FIFO_EN WTM_EN 0 0 HP_CLICK HPIS1 HPIS2
		BOOT - Reboot memory content (0: normal, 1: reboot)
		FIFO_EN - Fifo enable (0: disable, 1: enable)
		WTM_EN - FIFO watermark enable (0: disable, 1: enable)
		HP_CLICK - HPF enabled for click (0: filter bypassed, 1: enabled)
		HPIS1 - HPF enabled for interrupt generator 1 (0: bypassed, 1: enabled)
		HPIS2 - HPF enabled for interrupt generator 2 (0: bypassed, 1 enabled)   */
		xmWriteByte(CTRL_REG0_XM, 0x00);
		
		/* CTRL_REG1_XM (0x20) (Default value: 0x07)
		Bits (7-0): AODR3 AODR2 AODR1 AODR0 BDU AZEN AYEN AXEN
		AODR[3:0] - select the acceleration data rate:
			0000=power down, 0001=3.125Hz, 0010=6.25Hz, 0011=12.5Hz, 
			0100=25Hz, 0101=50Hz, 0110=100Hz, 0111=200Hz, 1000=400Hz,
			1001=800Hz, 1010=1600Hz, (remaining combinations undefined).
		BDU - block data update for accel AND mag
			0: Continuous update
			1: Output registers aren't updated until MSB and LSB have been read.
		AZEN, AYEN, and AXEN - Acceleration x/y/z-axis enabled.
			0: Axis disabled, 1: Axis enabled									 */	
		xmWriteByte(CTRL_REG1_XM, 0x57); // 100Hz data rate, x/y/z all enabled
		
		//Serial.println(xmReadByte(CTRL_REG1_XM));
		/* CTRL_REG2_XM (0x21) (Default value: 0x00)
		Bits (7-0): ABW1 ABW0 AFS2 AFS1 AFS0 AST1 AST0 SIM
		ABW[1:0] - Accelerometer anti-alias filter bandwidth
			00=773Hz, 01=194Hz, 10=362Hz, 11=50Hz
		AFS[2:0] - Accel full-scale selection
			000=+/-2g, 001=+/-4g, 010=+/-6g, 011=+/-8g, 100=+/-16g
		AST[1:0] - Accel self-test enable
			00=normal (no self-test), 01=positive st, 10=negative st, 11=not allowed
		SIM - SPI mode selection
			0=4-wire, 1=3-wire													 */
		xmWriteByte(CTRL_REG2_XM, 0x00); // Set scale to 2g
		
		/* CTRL_REG3_XM is used to set interrupt generators on INT1_XM
		Bits (7-0): P1_BOOT P1_TAP P1_INT1 P1_INT2 P1_INTM P1_DRDYA P1_DRDYM P1_EMPTY
		*/
		// Accelerometer data ready on INT1_XM (0x04)
		xmWriteByte(CTRL_REG3_XM, 0x04); 
	}
	
	// initMag() -- Sets up the magnetometer to begin reading.
	// This function steps through all magnetometer-related control registers.
	// Upon exit these registers will be set as:
	//	- CTRL_REG4_XM = 0x04: Mag data ready signal on INT2_XM pin.
	//	- CTRL_REG5_XM = 0x14: 100 Hz update rate. Low resolution. Interrupt
	//		requests don't latch. Temperature sensor disabled.
	//	- CTRL_REG6_XM = 0x00:  2 Gs scale.
	//	- CTRL_REG7_XM = 0x00: Continuous conversion mode. Normal HPF mode.
	//	- INT_CTRL_REG_M = 0x09: Interrupt active-high. Enable interrupts.
	void initMag()
	{	
		/* CTRL_REG5_XM enables temp sensor, sets mag resolution and data rate
		Bits (7-0): TEMP_EN M_RES1 M_RES0 M_ODR2 M_ODR1 M_ODR0 LIR2 LIR1
		TEMP_EN - Enable temperature sensor (0=disabled, 1=enabled)
		M_RES[1:0] - Magnetometer resolution select (0=low, 3=high)
		M_ODR[2:0] - Magnetometer data rate select
			000=3.125Hz, 001=6.25Hz, 010=12.5Hz, 011=25Hz, 100=50Hz, 101=100Hz
		LIR2 - Latch interrupt request on INT2_SRC (cleared by reading INT2_SRC)
			0=interrupt request not latched, 1=interrupt request latched
		LIR1 - Latch interrupt request on INT1_SRC (cleared by readging INT1_SRC)
			0=irq not latched, 1=irq latched 									 */
		xmWriteByte(CTRL_REG5_XM, 0x94); // Mag data rate - 100 Hz, enable temperature sensor
		
		/* CTRL_REG6_XM sets the magnetometer full-scale
		Bits (7-0): 0 MFS1 MFS0 0 0 0 0 0
		MFS[1:0] - Magnetic full-scale selection
		00:+/-2Gauss, 01:+/-4Gs, 10:+/-8Gs, 11:+/-12Gs							 */
		xmWriteByte(CTRL_REG6_XM, 0x00); // Mag scale to +/- 2GS
		
		/* CTRL_REG7_XM sets magnetic sensor mode, low power mode, and filters
		AHPM1 AHPM0 AFDS 0 0 MLP MD1 MD0
		AHPM[1:0] - HPF mode selection
			00=normal (resets reference registers), 01=reference signal for filtering, 
			10=normal, 11=autoreset on interrupt event
		AFDS - Filtered acceleration data selection
			0=internal filter bypassed, 1=data from internal filter sent to FIFO
		MLP - Magnetic data low-power mode
			0=data rate is set by M_ODR bits in CTRL_REG5
			1=data rate is set to 3.125Hz
		MD[1:0] - Magnetic sensor mode selection (default 10)
			00=continuous-conversion, 01=single-conversion, 10 and 11=power-down */
		xmWriteByte(CTRL_REG7_XM, 0x00); // Continuous conversion mode
		
		/* CTRL_REG4_XM is used to set interrupt generators on INT2_XM
		Bits (7-0): P2_TAP P2_INT1 P2_INT2 P2_INTM P2_DRDYA P2_DRDYM P2_Overrun P2_WTM
		*/
		xmWriteByte(CTRL_REG4_XM, 0x04); // Magnetometer data ready on INT2_XM (0x08)
		
		/* INT_CTRL_REG_M to set push-pull/open drain, and active-low/high
		Bits[7:0] - XMIEN YMIEN ZMIEN PP_OD IEA IEL 4D MIEN
		XMIEN, YMIEN, ZMIEN - Enable interrupt recognition on axis for mag data
		PP_OD - Push-pull/open-drain interrupt configuration (0=push-pull, 1=od)
		IEA - Interrupt polarity for accel and magneto
			0=active-low, 1=active-high
		IEL - Latch interrupt request for accel and magneto
			0=irq not latched, 1=irq latched
		4D - 4D enable. 4D detection is enabled when 6D bit in INT_GEN1_REG is set
		MIEN - Enable interrupt generation for magnetic data
			0=disable, 1=enable) */
		xmWriteByte(INT_CTRL_REG_M, 0x09); // Enable interrupts for mag, active-low, push-pull
	}
	
	// gReadByte() -- Reads a byte from a specified gyroscope register.
	// Input:
	// 	- subAddress = Register to be read from.
	// Output:
	// 	- An 8-bit value read from the requested address.
	uint8_t gReadByte(uint8_t subAddress)
	{
	  return gyro->readReg(subAddress);
	}
	// gReadBytes() -- Reads a number of bytes -- beginning at an address
	// and incrementing from there -- from the gyroscope.
	// Input:
	// 	- subAddress = Register to be read from.
	// 	- * dest = A pointer to an array of uint8_t's. Values read will be
	//		stored in here on return.
	//	- count = The number of bytes to be read.
	// Output: No value is returned, but the `dest` array will store
	// 	the data read upon exit.
	void gReadBytes(uint8_t subAddress, uint8_t * dest, uint8_t count)
	{
	  gyro->readBytesReg((subAddress|0x80), dest, count);
	}
	// gWriteByte() -- Write a byte to a register in the gyroscope.
	// Input:
	//	- subAddress = Register to be written to.
	//	- data = data to be written to the register.
	void gWriteByte(uint8_t subAddress, uint8_t data)
	{
	  gyro->writeReg(subAddress, data);
	}
	// xmReadByte() -- Read a byte from a register in the accel/mag sensor
	// Input:
	//	- subAddress = Register to be read from.
	// Output:
	//	- An 8-bit value read from the requested register.
	uint8_t xmReadByte(uint8_t subAddress)
	{
	  return xm->readReg(subAddress);
	}
	// xmReadBytes() -- Reads a number of bytes -- beginning at an address
	// and incrementing from there -- from the accelerometer/magnetometer.
	// Input:
	// 	- subAddress = Register to be read from.
	// 	- * dest = A pointer to an array of uint8_t's. Values read will be
	//		stored in here on return.
	//	- count = The number of bytes to be read.
	// Output: No value is returned, but the `dest` array will store
	// 	the data read upon exit.
	void xmReadBytes(uint8_t subAddress, uint8_t * dest, uint8_t count)
	{
	  xm->readBytesReg((subAddress|0x80), dest, count);
	}
	// xmWriteByte() -- Write a byte to a register in the accel/mag sensor.
	// Input:
	//	- subAddress = Register to be written to.
	//	- data = data to be written to the register.
	void xmWriteByte(uint8_t subAddress, uint8_t data)
	{
	  xm->writeReg(subAddress, data);
	}
	// calcgRes() -- Calculate the resolution of the gyroscope.
	// This function will set the value of the gRes variable. gScale must
	// be set prior to calling this function.
	void calcgRes()
	{
		// Possible gyro scales (and their register bit settings) are:
		// 245 DPS (00), 500 DPS (01), 2000 DPS (10). Here's a bit of an algorithm
		// to calculate DPS/(ADC tick) based on that 2-bit value:
		switch (gScale)
		{
		case G_SCALE_245DPS:
			gRes = 245.0 / 32768.0;
			break;
		case G_SCALE_500DPS:
			gRes = 500.0 / 32768.0;
			break;
		case G_SCALE_2000DPS:
			gRes = 2000.0 / 32768.0;
			break;
		}
	}
	
	// calcmRes() -- Calculate the resolution of the magnetometer.
	// This function will set the value of the mRes variable. mScale must
	// be set prior to calling this function.
	void calcmRes()
	{
		// Possible magnetometer scales (and their register bit settings) are:
		// 2 Gs (00), 4 Gs (01), 8 Gs (10) 12 Gs (11). Here's a bit of an algorithm
		// to calculate Gs/(ADC tick) based on that 2-bit value:
		mRes = mScale == M_SCALE_2GS ? 2.0 / 32768.0 : 
			   (float) (mScale << 2) / 32768.0;
	}
	
	// calcaRes() -- Calculate the resolution of the accelerometer.
	// This function will set the value of the aRes variable. aScale must
	// be set prior to calling this function.
	void calcaRes()
	{
		// Possible accelerometer scales (and their register bit settings) are:
		// 2 g (000), 4g (001), 6g (010) 8g (011), 16g (100). Here's a bit of an 
		// algorithm to calculate g/(ADC tick) based on that 3-bit value:
		aRes = aScale == A_SCALE_16G ? 16.0 / 32768.0 : 
			   (((float) aScale + 1.0) * 2.0) / 32768.0;
	}
	
	/*
	
	
	// Implementation of Sebastian Madgwick's "...efficient orientation filter for... inertial/magnetic sensor arrays"
	// (see http://www.x-io.co.uk/category/open-source/ for examples and more details)
	// which fuses acceleration, rotation rate, and magnetic moments to produce a quaternion-based estimate of absolute
	// device orientation -- which can be converted to yaw, pitch, and roll. Useful for stabilizing quadcopters, etc.
	// The performance of the orientation filter is at least as good as conventional Kalman-based filtering algorithms
	// but is much less computationally intensive---it can be performed on a 3.3 V Pro Mini operating at 8 MHz!
	void MadgwickQuaternionUpdate(float $ax, float $ay, float $az, float $gx, float $gy, float $gz, float $mx, float $my, float $mz)
	// Sensors x- and y-axes are aligned but magnetometer z-axis (+ down) is opposite to z-axis (+ up) of accelerometer and gyro!
	// This is ok by aircraft orientation standards!  
	// Pass gyro rate as rad/s
	{
		float q1 = q[0], q2 = q[1], q3 = q[2], q4 = q[3];   // short name local variable for readability
		float norm;
		float hx, hy, _2bx, _2bz;
		float s1, s2, s3, s4;
		float qDot1, qDot2, qDot3, qDot4;

		// Auxiliary variables to avoid repeated arithmetic
		float _2q1mx;
		float _2q1my;
		float _2q1mz;
		float _2q2mx;
		float _4bx;
		float _4bz;
		float _2q1 = 2.0f * q1;
		float _2q2 = 2.0f * q2;
		float _2q3 = 2.0f * q3;
		float _2q4 = 2.0f * q4;
		float _2q1q3 = 2.0f * q1 * q3;
		float _2q3q4 = 2.0f * q3 * q4;
		float q1q1 = q1 * q1;
		float q1q2 = q1 * q2;
		float q1q3 = q1 * q3;
		float q1q4 = q1 * q4;
		float q2q2 = q2 * q2;
		float q2q3 = q2 * q3;
		float q2q4 = q2 * q4;
		float q3q3 = q3 * q3;
		float q3q4 = q3 * q4;
		float q4q4 = q4 * q4;

		// Normalise accelerometer measurement
		norm = sqrt($ax * $ax + $ay * $ay + $az * $az);
		if (norm == 0.0f) return; // handle NaN
		norm = 1.0f/norm;
		$ax *= norm;
		$ay *= norm;
		$az *= norm;

		// Normalise magnetometer measurement
		norm = sqrt($mx * $mx + $my * $my + $mz * $mz);
		if (norm == 0.0f) return; // handle NaN
		norm = 1.0f/norm;
		$mx *= norm;
		$my *= norm;
		$mz *= norm;

		// Reference direction of Earth's magnetic field
		_2q1mx = 2.0f * q1 * $mx;
		_2q1my = 2.0f * q1 * $my;
		_2q1mz = 2.0f * q1 * $mz;
		_2q2mx = 2.0f * q2 * $mx;
		hx = $mx * q1q1 - _2q1my * q4 + _2q1mz * q3 + $mx * q2q2 + _2q2 * $my * q3 + _2q2 * $mz * q4 - $mx * q3q3 - $mx * q4q4;
		hy = _2q1mx * q4 + $my * q1q1 - _2q1mz * q2 + _2q2mx * q3 - $my * q2q2 + $my * q3q3 + _2q3 * $mz * q4 - $my * q4q4;
		_2bx = sqrt(hx * hx + hy * hy);
		_2bz = -_2q1mx * q3 + _2q1my * q2 + $mz * q1q1 + _2q2mx * q4 - $mz * q2q2 + _2q3 * $my * q4 - $mz * q3q3 + $mz * q4q4;
		_4bx = 2.0f * _2bx;
		_4bz = 2.0f * _2bz;

		// Gradient decent algorithm corrective step
		s1 = -_2q3 * (2.0f * q2q4 - _2q1q3 - $ax) + _2q2 * (2.0f * q1q2 + _2q3q4 - $ay) - _2bz * q3 * (_2bx * (0.5f - q3q3 - q4q4) + _2bz * (q2q4 - q1q3) - $mx) + (-_2bx * q4 + _2bz * q2) * (_2bx * (q2q3 - q1q4) + _2bz * (q1q2 + q3q4) - $my) + _2bx * q3 * (_2bx * (q1q3 + q2q4) + _2bz * (0.5f - q2q2 - q3q3) - $mz);
		s2 =  _2q4 * (2.0f * q2q4 - _2q1q3 - $ax) + _2q1 * (2.0f * q1q2 + _2q3q4 - $ay) - 4.0f * q2 * (1.0f - 2.0f * q2q2 - 2.0f * q3q3 - $az) + _2bz * q4 * (_2bx * (0.5f - q3q3 - q4q4) + _2bz * (q2q4 - q1q3) - $mx) + (_2bx * q3 + _2bz * q1) * (_2bx * (q2q3 - q1q4) + _2bz * (q1q2 + q3q4) - $my) + (_2bx * q4 - _4bz * q2) * (_2bx * (q1q3 + q2q4) + _2bz * (0.5f - q2q2 - q3q3) - $mz);
		s3 = -_2q1 * (2.0f * q2q4 - _2q1q3 - $ax) + _2q4 * (2.0f * q1q2 + _2q3q4 - $ay) - 4.0f * q3 * (1.0f - 2.0f * q2q2 - 2.0f * q3q3 - $az) + (-_4bx * q3 - _2bz * q1) * (_2bx * (0.5f - q3q3 - q4q4) + _2bz * (q2q4 - q1q3) - $mx) + (_2bx * q2 + _2bz * q4) * (_2bx * (q2q3 - q1q4) + _2bz * (q1q2 + q3q4) - $my) + (_2bx * q1 - _4bz * q3) * (_2bx * (q1q3 + q2q4) + _2bz * (0.5f - q2q2 - q3q3) - $mz);
		s4 =  _2q2 * (2.0f * q2q4 - _2q1q3 - $ax) + _2q3 * (2.0f * q1q2 + _2q3q4 - $ay) + (-_4bx * q4 + _2bz * q2) * (_2bx * (0.5f - q3q3 - q4q4) + _2bz * (q2q4 - q1q3) - $mx) + (-_2bx * q1 + _2bz * q3) * (_2bx * (q2q3 - q1q4) + _2bz * (q1q2 + q3q4) - $my) + _2bx * q2 * (_2bx * (q1q3 + q2q4) + _2bz * (0.5f - q2q2 - q3q3) - $mz);
		norm = sqrt(s1 * s1 + s2 * s2 + s3 * s3 + s4 * s4);    // normalise step magnitude
		norm = 1.0f/norm;
		s1 *= norm;
		s2 *= norm;
		s3 *= norm;
		s4 *= norm;

		// Compute rate of change of quaternion
		qDot1 = 0.5f * (-q2 * $gx - q3 * $gy - q4 * $gz) - beta * s1;
		qDot2 = 0.5f * ( q1 * $gx + q3 * $gz - q4 * $gy) - beta * s2;
		qDot3 = 0.5f * ( q1 * $gy - q2 * $gz + q4 * $gx) - beta * s3;
		qDot4 = 0.5f * ( q1 * $gz + q2 * $gy - q3 * $gx) - beta * s4;

		// Integrate to yield quaternion
		q1 += qDot1 * deltat;
		q2 += qDot2 * deltat;
		q3 += qDot3 * deltat;
		q4 += qDot4 * deltat;
		norm = sqrt(q1 * q1 + q2 * q2 + q3 * q3 + q4 * q4);    // normalise quaternion
		norm = 1.0f/norm;
		q[0] = q1 * norm;
		q[1] = q2 * norm;
		q[2] = q3 * norm;
		q[3] = q4 * norm;

	}

	
	void MahonyQuaternionUpdate(float AX, float AY, float AZ, float GX, float GY, float GZ, float MX, float MY, float MZ)
	{
		float q1 = q[0], q2 = q[1], q3 = q[2], q4 = q[3];   // short name local variable for readability
		float norm;
		float hx, hy, bx, bz;
		float vx, vy, vz, wx, wy, wz;
		float ex, ey, ez;
		float pa, pb, pc;

		// Auxiliary variables to avoid repeated arithmetic
		float q1q1 = q1 * q1;
		float q1q2 = q1 * q2;
		float q1q3 = q1 * q3;
		float q1q4 = q1 * q4;
		float q2q2 = q2 * q2;
		float q2q3 = q2 * q3;
		float q2q4 = q2 * q4;
		float q3q3 = q3 * q3;
		float q3q4 = q3 * q4;
		float q4q4 = q4 * q4;   

		// Normalise accelerometer measurement
		norm = sqrt(AX * AX + AY * AY + AZ * AZ);
		if (norm == 0.0f) return; // handle NaN
		norm = 1.0f / norm;        // use reciprocal for division
		AX *= norm;
		AY *= norm;
		AZ *= norm;

		// Normalise magnetometer measurement
		norm = sqrt(MX * MX + MY * MY + MZ * MZ);
		if (norm == 0.0f) return; // handle NaN
		norm = 1.0f / norm;        // use reciprocal for division
		MX *= norm;
		MY *= norm;
		MZ *= norm;

		// Reference direction of Earth's magnetic field
		hx = 2.0f * MX * (0.5f - q3q3 - q4q4) + 2.0f * MY * (q2q3 - q1q4) + 2.0f * MZ * (q2q4 + q1q3);
		hy = 2.0f * MX * (q2q3 + q1q4) + 2.0f * MY * (0.5f - q2q2 - q4q4) + 2.0f * MZ * (q3q4 - q1q2);
		bx = sqrt((hx * hx) + (hy * hy));
		bz = 2.0f * MX * (q2q4 - q1q3) + 2.0f * MY * (q3q4 + q1q2) + 2.0f * MZ * (0.5f - q2q2 - q3q3);

		// Estimated direction of gravity and magnetic field
		vx = 2.0f * (q2q4 - q1q3);
		vy = 2.0f * (q1q2 + q3q4);
		vz = q1q1 - q2q2 - q3q3 + q4q4;
		wx = 2.0f * bx * (0.5f - q3q3 - q4q4) + 2.0f * bz * (q2q4 - q1q3);
		wy = 2.0f * bx * (q2q3 - q1q4) + 2.0f * bz * (q1q2 + q3q4);
		wz = 2.0f * bx * (q1q3 + q2q4) + 2.0f * bz * (0.5f - q2q2 - q3q3);  

		// Error is cross product between estimated direction and measured direction of gravity
		ex = (AY * vz - AZ * vy) + (MY * wz - MZ * wy);
		ey = (AZ * vx - AX * vz) + (MZ * wx - MX * wz);
		ez = (AX * vy - AY * vx) + (MX * wy - MY * wx);
		if (Ki > 0.0f)
		{
			eInt[0] += ex;      // accumulate integral error
			eInt[1] += ey;
			eInt[2] += ez;
		}
		else
		{
			eInt[0] = 0.0f;     // prevent integral wind up
			eInt[1] = 0.0f;
			eInt[2] = 0.0f;
		}

		// Apply feedback terms
		GX = GX + Kp * ex + Ki * eInt[0];
		GY = GY + Kp * ey + Ki * eInt[1];
		GZ = GZ + Kp * ez + Ki * eInt[2];

		// Integrate rate of change of quaternion
		pa = q2;
		pb = q3;
		pc = q4;
		q1 = q1 + (-q2 * GX - q3 * GY - q4 * GZ) * (0.5f * deltat);
		q2 = pa + ( q1 * GX + pb * GZ - pc * GY) * (0.5f * deltat);
		q3 = pb + ( q1 * GY - pa * GZ + pc * GX) * (0.5f * deltat);
		q4 = pc + ( q1 * GZ + pa * GY - pb * GX) * (0.5f * deltat);

		// Normalise quaternion
		norm = sqrt(q1 * q1 + q2 * q2 + q3 * q3 + q4 * q4);
		norm = 1.0f / norm;
		q[0] = q1 * norm;
		q[1] = q2 * norm;
		q[2] = q3 * norm;
		q[3] = q4 * norm;

	}
	
	*/
	
	void MadgwickAHRSupdate(float gx, float gy, float gz, float ax, float ay, float az, float mx, float my, float mz, float delta) 
	{
		float recipNorm;
		float s0, s1, s2, s3;
		float qDot1, qDot2, qDot3, qDot4;
		float hx, hy;
		float _2q0mx, _2q0my, _2q0mz, _2q1mx, _2bx, _2bz, _4bx, _4bz, _2q0, _2q1, _2q2, _2q3, _2q0q2, _2q2q3, q0q0, q0q1, q0q2, q0q3, q1q1, q1q2, q1q3, q2q2, q2q3, q3q3;

		// Use IMU algorithm if magnetometer measurement invalid (avoids NaN in magnetometer normalisation)
		if((mx == 0.0f) && (my == 0.0f) && (mz == 0.0f)) {
			MadgwickAHRSupdateIMU(gx, gy, gz, ax, ay, az, delta);
			return;
		}

		// Rate of change of quaternion from gyroscope
		qDot1 = 0.5f * (-q1 * gx - q2 * gy - q3 * gz);
		qDot2 = 0.5f * (q0 * gx + q2 * gz - q3 * gy);
		qDot3 = 0.5f * (q0 * gy - q1 * gz + q3 * gx);
		qDot4 = 0.5f * (q0 * gz + q1 * gy - q2 * gx);

		// Compute feedback only if accelerometer measurement valid (avoids NaN in accelerometer normalisation)
		if(!((ax == 0.0f) && (ay == 0.0f) && (az == 0.0f))) {

			// Normalise accelerometer measurement
			recipNorm = invSqrt(ax * ax + ay * ay + az * az);
			ax *= recipNorm;
			ay *= recipNorm;
			az *= recipNorm;   

			// Normalise magnetometer measurement
			recipNorm = invSqrt(mx * mx + my * my + mz * mz);
			mx *= recipNorm;
			my *= recipNorm;
			mz *= recipNorm;

			// Auxiliary variables to avoid repeated arithmetic
			_2q0mx = 2.0f * q0 * mx;
			_2q0my = 2.0f * q0 * my;
			_2q0mz = 2.0f * q0 * mz;
			_2q1mx = 2.0f * q1 * mx;
			_2q0 = 2.0f * q0;
			_2q1 = 2.0f * q1;
			_2q2 = 2.0f * q2;
			_2q3 = 2.0f * q3;
			_2q0q2 = 2.0f * q0 * q2;
			_2q2q3 = 2.0f * q2 * q3;
			q0q0 = q0 * q0;
			q0q1 = q0 * q1;
			q0q2 = q0 * q2;
			q0q3 = q0 * q3;
			q1q1 = q1 * q1;
			q1q2 = q1 * q2;
			q1q3 = q1 * q3;
			q2q2 = q2 * q2;
			q2q3 = q2 * q3;
			q3q3 = q3 * q3;

			// Reference direction of Earth's magnetic field
			hx = mx * q0q0 - _2q0my * q3 + _2q0mz * q2 + mx * q1q1 + _2q1 * my * q2 + _2q1 * mz * q3 - mx * q2q2 - mx * q3q3;
			hy = _2q0mx * q3 + my * q0q0 - _2q0mz * q1 + _2q1mx * q2 - my * q1q1 + my * q2q2 + _2q2 * mz * q3 - my * q3q3;
			_2bx = sqrt(hx * hx + hy * hy);
			_2bz = -_2q0mx * q2 + _2q0my * q1 + mz * q0q0 + _2q1mx * q3 - mz * q1q1 + _2q2 * my * q3 - mz * q2q2 + mz * q3q3;
			_4bx = 2.0f * _2bx;
			_4bz = 2.0f * _2bz;

			// Gradient decent algorithm corrective step
			s0 = -_2q2 * (2.0f * q1q3 - _2q0q2 - ax) + _2q1 * (2.0f * q0q1 + _2q2q3 - ay) - _2bz * q2 * (_2bx * (0.5f - q2q2 - q3q3) + _2bz * (q1q3 - q0q2) - mx) + (-_2bx * q3 + _2bz * q1) * (_2bx * (q1q2 - q0q3) + _2bz * (q0q1 + q2q3) - my) + _2bx * q2 * (_2bx * (q0q2 + q1q3) + _2bz * (0.5f - q1q1 - q2q2) - mz);
			s1 = _2q3 * (2.0f * q1q3 - _2q0q2 - ax) + _2q0 * (2.0f * q0q1 + _2q2q3 - ay) - 4.0f * q1 * (1 - 2.0f * q1q1 - 2.0f * q2q2 - az) + _2bz * q3 * (_2bx * (0.5f - q2q2 - q3q3) + _2bz * (q1q3 - q0q2) - mx) + (_2bx * q2 + _2bz * q0) * (_2bx * (q1q2 - q0q3) + _2bz * (q0q1 + q2q3) - my) + (_2bx * q3 - _4bz * q1) * (_2bx * (q0q2 + q1q3) + _2bz * (0.5f - q1q1 - q2q2) - mz);
			s2 = -_2q0 * (2.0f * q1q3 - _2q0q2 - ax) + _2q3 * (2.0f * q0q1 + _2q2q3 - ay) - 4.0f * q2 * (1 - 2.0f * q1q1 - 2.0f * q2q2 - az) + (-_4bx * q2 - _2bz * q0) * (_2bx * (0.5f - q2q2 - q3q3) + _2bz * (q1q3 - q0q2) - mx) + (_2bx * q1 + _2bz * q3) * (_2bx * (q1q2 - q0q3) + _2bz * (q0q1 + q2q3) - my) + (_2bx * q0 - _4bz * q2) * (_2bx * (q0q2 + q1q3) + _2bz * (0.5f - q1q1 - q2q2) - mz);
			s3 = _2q1 * (2.0f * q1q3 - _2q0q2 - ax) + _2q2 * (2.0f * q0q1 + _2q2q3 - ay) + (-_4bx * q3 + _2bz * q1) * (_2bx * (0.5f - q2q2 - q3q3) + _2bz * (q1q3 - q0q2) - mx) + (-_2bx * q0 + _2bz * q2) * (_2bx * (q1q2 - q0q3) + _2bz * (q0q1 + q2q3) - my) + _2bx * q1 * (_2bx * (q0q2 + q1q3) + _2bz * (0.5f - q1q1 - q2q2) - mz);
			recipNorm = invSqrt(s0 * s0 + s1 * s1 + s2 * s2 + s3 * s3); // normalise step magnitude
			s0 *= recipNorm;
			s1 *= recipNorm;
			s2 *= recipNorm;
			s3 *= recipNorm;

			// Apply feedback step
			qDot1 -= beta * s0;
			qDot2 -= beta * s1;
			qDot3 -= beta * s2;
			qDot4 -= beta * s3;
		}

		// Integrate rate of change of quaternion to yield quaternion
		q0 += qDot1 * delta; //(1.0f / sampleFreq);
		q1 += qDot2 * delta; //(1.0f / sampleFreq);
		q2 += qDot3 * delta; //(1.0f / sampleFreq);
		q3 += qDot4 * delta; //(1.0f / sampleFreq);

		// Normalise quaternion
		recipNorm = invSqrt(q0 * q0 + q1 * q1 + q2 * q2 + q3 * q3);
		q0 *= recipNorm;
		q1 *= recipNorm;
		q2 *= recipNorm;
		q3 *= recipNorm;
	}

	//---------------------------------------------------------------------------------------------------
	// IMU algorithm update

	void MadgwickAHRSupdateIMU(float gx, float gy, float gz, float ax, float ay, float az, float delta) 
	{
		float recipNorm;
		float s0, s1, s2, s3;
		float qDot1, qDot2, qDot3, qDot4;
		float _2q0, _2q1, _2q2, _2q3, _4q0, _4q1, _4q2 ,_8q1, _8q2, q0q0, q1q1, q2q2, q3q3;

		// Rate of change of quaternion from gyroscope
		qDot1 = 0.5f * (-q1 * gx - q2 * gy - q3 * gz);
		qDot2 = 0.5f * (q0 * gx + q2 * gz - q3 * gy);
		qDot3 = 0.5f * (q0 * gy - q1 * gz + q3 * gx);
		qDot4 = 0.5f * (q0 * gz + q1 * gy - q2 * gx);

		// Compute feedback only if accelerometer measurement valid (avoids NaN in accelerometer normalisation)
		if(!((ax == 0.0f) && (ay == 0.0f) && (az == 0.0f))) {

			// Normalise accelerometer measurement
			recipNorm = invSqrt(ax * ax + ay * ay + az * az);
			ax *= recipNorm;
			ay *= recipNorm;
			az *= recipNorm;   

			// Auxiliary variables to avoid repeated arithmetic
			_2q0 = 2.0f * q0;
			_2q1 = 2.0f * q1;
			_2q2 = 2.0f * q2;
			_2q3 = 2.0f * q3;
			_4q0 = 4.0f * q0;
			_4q1 = 4.0f * q1;
			_4q2 = 4.0f * q2;
			_8q1 = 8.0f * q1;
			_8q2 = 8.0f * q2;
			q0q0 = q0 * q0;
			q1q1 = q1 * q1;
			q2q2 = q2 * q2;
			q3q3 = q3 * q3;

			// Gradient decent algorithm corrective step
			s0 = _4q0 * q2q2 + _2q2 * ax + _4q0 * q1q1 - _2q1 * ay;
			s1 = _4q1 * q3q3 - _2q3 * ax + 4.0f * q0q0 * q1 - _2q0 * ay - _4q1 + _8q1 * q1q1 + _8q1 * q2q2 + _4q1 * az;
			s2 = 4.0f * q0q0 * q2 + _2q0 * ax + _4q2 * q3q3 - _2q3 * ay - _4q2 + _8q2 * q1q1 + _8q2 * q2q2 + _4q2 * az;
			s3 = 4.0f * q1q1 * q3 - _2q1 * ax + 4.0f * q2q2 * q3 - _2q2 * ay;
			recipNorm = invSqrt(s0 * s0 + s1 * s1 + s2 * s2 + s3 * s3); // normalise step magnitude
			s0 *= recipNorm;
			s1 *= recipNorm;
			s2 *= recipNorm;
			s3 *= recipNorm;

			// Apply feedback step
			qDot1 -= beta * s0;
			qDot2 -= beta * s1;
			qDot3 -= beta * s2;
			qDot4 -= beta * s3;
		}

		// Integrate rate of change of quaternion to yield quaternion
		q0 += qDot1 * delta; //(1.0f / sampleFreq);
		q1 += qDot2 * delta; //(1.0f / sampleFreq);
		q2 += qDot3 * delta; //(1.0f / sampleFreq);
		q3 += qDot4 * delta; //(1.0f / sampleFreq);

		// Normalise quaternion
		recipNorm = invSqrt(q0 * q0 + q1 * q1 + q2 * q2 + q3 * q3);
		q0 *= recipNorm;
		q1 *= recipNorm;
		q2 *= recipNorm;
		q3 *= recipNorm;
	}

	float invSqrt(float x) //x-io
	{
		float halfx = 0.5f * x;
		float y = x;
		long i = *(long*)&y;
		i = 0x5f3759df - (i>>1);
		y = *(float*)&i;
		y = y * (1.5f - (halfx * y * y));
		return y;
	}
	
	// Define output variables from updated quaternion---these are Tait-Bryan angles, commonly used in aircraft orientation.
	// In this coordinate system, the positive z-axis is down toward Earth. 
	// Yaw is the angle between Sensor x-axis and Earth magnetic North (or true North if corrected for local declination), 
	// looking down on the sensor positive yaw is counterclockwise.
	// Pitch is angle between sensor x-axis and Earth ground plane, toward the Earth is positive, up toward the sky is negative.
	// Roll is angle between sensor y-axis and Earth ground plane, y-axis up is positive roll.
	// These arise from the definition of the homogeneous rotation matrix constructed from quaternions.
	// Tait-Bryan angles as well as Euler angles are non-commutative; that is, to get the correct orientation the rotations must be
	// applied in the correct order which for this configuration is yaw, pitch, and then roll.
	// For more see http://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles which has additional links.
	void CalcAngles(float q0, float q1, float q2, float q3, float declination)
	{
		yaw   = atan2(2.0f * (q1 * q2 + q0 * q3), q0 * q0 + q1 * q1 - q2 * q2 - q3 * q3);   
		pitch = -asin(2.0f * (q1 * q3 - q0 * q2));
		roll  = atan2(2.0f * (q0 * q1 + q2 * q3), q0 * q0 - q1 * q1 - q2 * q2 + q3 * q3);
		pitch *= 180.0f / M_PI;
		yaw   *= 180.0f / M_PI; 
		yaw   -= declination;
		roll  *= 180.0f / M_PI;
	}

};

#endif // SFE_LSM9DS0_H //
