/*
 * ControllerQuad.h
 *
 *  Created on: Jun 30, 2011
 *      Author: jgoppert
 */

#ifndef CONTROLLERQUAD_H_
#define CONTROLLERQUAD_H_

#include "../APO/AP_Controller.h"

namespace apo {

class ControllerQuad: public AP_Controller {
public:

	/**
	 * note that these are not the controller radio channel numbers, they are just
	 * unique keys so they can be reaccessed from the hal rc vector
	 */
	enum {
		CH_MODE = 0, // note scicoslab channels set mode, left, right, front, back order
		CH_LEFT, // this enum must match this
		CH_RIGHT,
		CH_FRONT,
		CH_BACK,
		CH_ROLL,
		CH_PITCH,
		CH_YAW,
		CH_THRUST
	};

	enum {
		k_chMode = k_radioChannelsStart,
		k_chLeft,
		k_chRight,
		k_chFront,
		k_chBack,
		k_chRoll,
		k_chPitch,
		k_chYaw,
		k_chThr
	};

	enum {
		k_pidGroundSpeed2Throttle = k_controllersStart,
		k_pidStr,
		k_pidPN,
		k_pidPE,
		k_pidPD,
		k_pidRoll,
		k_pidPitch,
		k_pidYawRate,
		k_pidYaw,
	};

	ControllerQuad(AP_Navigator * nav, AP_Guide * guide,
			AP_HardwareAbstractionLayer * hal) :
				AP_Controller(nav, guide, hal),
				pidRoll(new AP_Var_group(k_pidRoll, PSTR("ROLL_")), 1,
						PID_ATT_P, PID_ATT_I, PID_ATT_D, PID_ATT_AWU,
						PID_ATT_LIM),
				pidPitch(new AP_Var_group(k_pidPitch, PSTR("PITCH_")), 1,
						PID_ATT_P, PID_ATT_I, PID_ATT_D, PID_ATT_AWU,
						PID_ATT_LIM),
				pidYaw(new AP_Var_group(k_pidYaw, PSTR("YAW_")), 1,
						PID_YAWPOS_P, PID_YAWPOS_I, PID_YAWPOS_D,
						PID_YAWPOS_AWU, PID_YAWPOS_LIM),
				pidYawRate(new AP_Var_group(k_pidYawRate, PSTR("YAWRT_")), 1,
						PID_YAWSPEED_P, PID_YAWSPEED_I, PID_YAWSPEED_D,
						PID_YAWSPEED_AWU, PID_YAWSPEED_LIM, PID_YAWSPEED_DFCUT),
				pidPN(new AP_Var_group(k_pidPN, PSTR("NORTH_")), 1, PID_POS_P,
						PID_POS_I, PID_POS_D, PID_POS_AWU, PID_POS_LIM),
				pidPE(new AP_Var_group(k_pidPE, PSTR("EAST_")), 1, PID_POS_P,
						PID_POS_I, PID_POS_D, PID_POS_AWU, PID_POS_LIM),
				pidPD(new AP_Var_group(k_pidPD, PSTR("DOWN_")), 1, PID_POS_Z_P,
						PID_POS_Z_I, PID_POS_Z_D, PID_POS_Z_AWU, PID_POS_Z_LIM) {
		/*
		 * allocate radio channels
		 * the order of the channels has to match the enumeration above
		 */
		_hal->rc.push_back(
				new AP_RcChannel(k_chMode, PSTR("MODE_"), APM_RC, 5, 1100,
						1500, 1900, RC_MODE_IN));
		_hal->rc.push_back(
				new AP_RcChannel(k_chLeft, PSTR("LEFT_"), APM_RC, 0, 1100,
						1100, 1900, RC_MODE_OUT));
		_hal->rc.push_back(
				new AP_RcChannel(k_chRight, PSTR("RIGHT_"), APM_RC, 1, 1100,
						1100, 1900, RC_MODE_OUT));
		_hal->rc.push_back(
				new AP_RcChannel(k_chFront, PSTR("FRONT_"), APM_RC, 2, 1100,
						1100, 1900, RC_MODE_OUT));
		_hal->rc.push_back(
				new AP_RcChannel(k_chBack, PSTR("BACK_"), APM_RC, 3, 1100,
						1100, 1900, RC_MODE_OUT));
		_hal->rc.push_back(
				new AP_RcChannel(k_chRoll, PSTR("ROLL_"), APM_RC, 0, 1100,
						1500, 1900, RC_MODE_IN));
		_hal->rc.push_back(
				new AP_RcChannel(k_chPitch, PSTR("PITCH_"), APM_RC, 1, 1100,
						1500, 1900, RC_MODE_IN));
		_hal->rc.push_back(
				new AP_RcChannel(k_chYaw, PSTR("YAW_"), APM_RC, 2, 1100, 1500,
						1900, RC_MODE_IN));
		_hal->rc.push_back(
				new AP_RcChannel(k_chThr, PSTR("THRUST_"), APM_RC, 3, 1100,
						1100, 1900, RC_MODE_IN));
	}

	virtual void update(const float & dt) {


		if (_hal->heartBeatLost()) {
			// heartbeat lost, go to failsafe mode
			_mode = MAV_MODE_FAILSAFE;
			setAllRadioChannelsToNeutral();
			_hal->setState(MAV_STATE_EMERGENCY);
			_hal->debug->printf_P(PSTR("comm lost, send heartbeat from gcs\n"));
			return;
		} else if (_hal->rc[CH_THRUST]->getRadioPosition() < 0.05) {
			// if throttle less than 5% cut motor power
			_mode = MAV_MODE_LOCKED;
			setAllRadioChannelsToNeutral();
			_hal->setState(MAV_STATE_STANDBY);
			return;
		} else if (_hal->getMode() == MODE_LIVE) {
			_hal->setState(MAV_STATE_ACTIVE);
		} else if (_hal->getMode() == MODE_HIL_CNTL) {
			_hal->setState(MAV_STATE_HILSIM);
		}

		// manual mode
		float mixRemoteWeight = 0;
		if (_hal->rc[CH_MODE]->getRadioPosition() > 0) {
			mixRemoteWeight = 1;
			_mode = MAV_MODE_MANUAL;
		} else {
			_mode = MAV_MODE_AUTO;
		}

		// commands for inner loop
		float cmdRoll = 0;
		float cmdPitch = 0;
		float cmdYawRate = 0;
		float thrustMix = 0;

		switch(_mode) {

		case MAV_MODE_MANUAL: {
			setAllRadioChannelsManually();
			// "mix manual"
			cmdRoll = 0.5 * _hal->rc[CH_ROLL]->getPosition()
					* mixRemoteWeight;
			cmdPitch = 0.5 * _hal->rc[CH_PITCH]->getPosition()
					* mixRemoteWeight;
			cmdYawRate = 0.5 * _hal->rc[CH_YAW]->getPosition()
					* mixRemoteWeight;
			thrustMix = _hal->rc[CH_THRUST]->getPosition() * mixRemoteWeight;
			break;
		}

		case MAV_MODE_AUTO: {

			// XXX kills all commands, 
			// auto not currently implemented
			cmdRoll = 0;
			cmdPitch = 0;
			cmdYawRate = 0;
			thrustMix = 0;

			// position loop
			/*
			 float cmdNorthTilt = pidPN.update(_nav->getPN(),_nav->getVN(),dt);
			 float cmdEastTilt = pidPE.update(_nav->getPE(),_nav->getVE(),dt);
			 float cmdDown = pidPD.update(_nav->getPD(),_nav->getVD(),dt);

			 // "transform-to-body"
			 {
			 float trigSin = sin(-yaw);
			 float trigCos = cos(-yaw);
			 _cmdPitch = _cmdEastTilt * trigCos
			 - _cmdNorthTilt * trigSin;
			 _cmdRoll = -_cmdEastTilt * trigSin
			 + _cmdNorthTilt * trigCos;
			 // note that the north tilt is negative of the pitch
			 }

			 //thrustMix += THRUST_HOVER_OFFSET;

			 // "thrust-trim-adjust"
			 if (fabs(_cmdRoll) > 0.5) {
			 _thrustMix *= 1.13949393;
			 } else {
			 _thrustMix /= cos(_cmdRoll);
			 }
			 if (fabs(_cmdPitch) > 0.5) {
			 _thrustMix *= 1.13949393;
			 } else {
			 _thrustMix /= cos(_cmdPitch);
			 }
			 */
		}

		}

		// attitude loop
		// XXX negative sign added to nav roll, not sure why this is necessary
		// XXX negative sign added to nav roll rate, not sure why this is necessary
		float rollMix = pidRoll.update(cmdRoll + _nav->getRoll(),
				-_nav->getRollRate(), dt);
		// XXX negative sign added to cmdPitch, not sure why this is necessary
		float pitchMix = pidPitch.update(-cmdPitch - _nav->getPitch(),
				_nav->getPitchRate(), dt);
		// XXX negative sign added to cmdYawRate, not sure why this is necessary
		float yawMix = pidYawRate.update(-cmdYawRate - _nav->getYawRate(), dt);

		_hal->rc[CH_LEFT]->setPosition(thrustMix + rollMix + yawMix);
		_hal->rc[CH_RIGHT]->setPosition(thrustMix - rollMix + yawMix);
		_hal->rc[CH_FRONT]->setPosition(thrustMix + pitchMix - yawMix);
		_hal->rc[CH_BACK]->setPosition(thrustMix - pitchMix - yawMix);

		//		_hal->debug->printf("L: %f\t R: %f\t F: %f\t B: %f\n",
		//				_hal->rc[CH_LEFT]->getPosition(),
		//				_hal->rc[CH_RIGHT]->getPosition(),
		//				_hal->rc[CH_FRONT]->getPosition(),
		//				_hal->rc[CH_BACK]->getPosition());

		_hal->debug->printf(
				"rollMix: %f\t pitchMix: %f\t yawMix: %f\t thrustMix: %f\n",
				rollMix, pitchMix, yawMix, thrustMix);

		//			_hal->debug->printf("roll pwm: %d\t pitch pwm: %d\t yaw pwm: %d\t thrust pwm: %d\n",
		//					_hal->rc[CH_ROLL]->readRadio(),
		//					_hal->rc[CH_PITCH]->readRadio(),
		//					_hal->rc[CH_YAW]->readRadio(),
		//					_hal->rc[CH_THRUST]->readRadio());
	}
	virtual MAV_MODE getMode() {
		return (MAV_MODE) _mode.get();
	}
private:
	AP_Uint8 _mode;
	BlockPIDDfb pidRoll, pidPitch, pidYaw;
	BlockPID pidYawRate;
	BlockPIDDfb pidPN, pidPE, pidPD;

};

} // namespace apo

#endif /* CONTROLLERQUAD_H_ */
