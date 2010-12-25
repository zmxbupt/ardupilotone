/*
	RC_ChannelB.cpp - Radio library for Arduino
	Code by Jason Short. DIYDrones.com
	
	This library is free software; you can redistribute it and / or
		modify it under the terms of the GNU Lesser General Public
		License as published by the Free Software Foundation; either
		version 2.1 of the License, or (at your option) any later version.

*/

#include <math.h>
#include <avr/eeprom.h>
#include "WProgram.h"
#include "RC_ChannelB.h"

#define ANGLE 0
#define RANGE 1

// setup the control preferences
void 	
RC_ChannelB::set_range(int low, int high)
{
	_type 	= RANGE;
	_high 	= high;
	_low 	= low;
}

void
RC_ChannelB::set_angle(int angle)
{
	_type 	= ANGLE;
	_high 	= angle;
}

void
RC_ChannelB::set_reverse(bool reverse)
{
	if (reverse) _reverse = -1;
	else _reverse = 1;
}

void
RC_ChannelB::set_filter(bool filter)
{
	_filter = filter;
}

// call after first read
void
RC_ChannelB::trim()
{
	radio_trim = radio_in;
	
}

// read input from APM_RC - create a control_in value
void
RC_ChannelB::set_pwm(int pwm)
{
	//Serial.print(pwm,DEC);

	if(_filter){
		if(radio_in == 0)
			radio_in = pwm;
		else
			radio_in = ((pwm + radio_in) >> 1);		// Small filtering
	}else{
		radio_in = pwm;
	}
	
	if(_type == RANGE){
		//Serial.print("range ");
		control_in = pwm_to_range();
		control_in = (control_in < dead_zone) ? 0 : control_in;
	}else{
		control_in = pwm_to_angle();
		control_in = (abs(control_in) < dead_zone) ? 0 : control_in;
	}
}

int
RC_ChannelB::control_mix(float value)
{
	return (1 - abs(control_in / _high)) * value + control_in;
}

// are we below a threshold?
bool
RC_ChannelB::get_failsafe(void)
{
	return (radio_in < (radio_min - 50));
}

// returns just the PWM without the offset from radio_min
void
RC_ChannelB::calc_pwm(void)
{

	if(_type == RANGE){
		pwm_out = range_to_pwm();
	}else{
		pwm_out = angle_to_pwm();
	}
	radio_out = pwm_out + radio_min;
}

// ------------------------------------------

void
RC_ChannelB::load_eeprom(void)
{
	radio_min 	= eeprom_read_word((uint16_t *)	_address);
	radio_max	= eeprom_read_word((uint16_t *)	(_address + 2));
	radio_trim 	= eeprom_read_word((uint16_t *)	(_address + 4));
}

void
RC_ChannelB::save_eeprom(void)
{
	eeprom_write_word((uint16_t *)	_address, 			radio_min);
	eeprom_write_word((uint16_t *)	(_address + 2), 	radio_max);
	eeprom_write_word((uint16_t *)	(_address + 4), 	radio_trim);
}

// ------------------------------------------
void
RC_ChannelB::save_trim(void)
{
	eeprom_write_word((uint16_t *)	(_address + 4), 	radio_trim);
}

// ------------------------------------------

void
RC_ChannelB::update_min_max()
{
	radio_min = min(radio_min, radio_in);
	radio_max = max(radio_max, radio_in);
}

// ------------------------------------------

int16_t 
RC_ChannelB::pwm_to_angle()
{
	if(radio_in < radio_trim)
		return _reverse * _high * ((float)(radio_in - radio_trim) / (float)(radio_trim - radio_min));
	else
		return _reverse * _high * ((float)(radio_in - radio_trim) / (float)(radio_max  - radio_trim));
}

float 
RC_ChannelB::norm_input()
{
	if(radio_in < radio_trim)
		return _reverse * (float)(radio_in - radio_trim) / (float)(radio_trim - radio_min);
	else
		return _reverse * (float)(radio_in - radio_trim) / (float)(radio_max  - radio_trim);
}

float 
RC_ChannelB::norm_output()
{
	if(radio_out < radio_trim)
		return (float)(radio_out - radio_trim) / (float)(radio_trim - radio_min);
	else
		return (float)(radio_out - radio_trim) / (float)(radio_max  - radio_trim);
}

int16_t
RC_ChannelB::angle_to_pwm()
{
	if(servo_out < 0)
		return (((float)servo_out / (float)_high) * (float)(radio_max - radio_trim));
	else
		return (((float)servo_out / (float)_high) * (float)(radio_trim - radio_min));
}

// ------------------------------------------

int16_t
RC_ChannelB::pwm_to_range()
{
	return _reverse * (_low + ((_high - _low) * ((float)(radio_in - radio_min) / (float)(radio_max - radio_min))));
}

int16_t
RC_ChannelB::range_to_pwm()
{
	return (((float)servo_out / (float)(_high - _low)) * (float)(radio_max - radio_min));
}

