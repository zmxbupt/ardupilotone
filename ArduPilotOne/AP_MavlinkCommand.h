/*
 * AP_MavlinkCommand.h
 *
 *  Created on: Apr 4, 2011
 *      Author: jgoppert
 */

#ifndef AP_MAVLINKCOMMAND_H_
#define AP_MAVLINKCOMMAND_H_

#include "AP_Var_keys.h"

class AP_MavlinkCommand {
private:
	struct CommandStorage {
		MAV_CMD command;
		bool autocontinue;
		MAV_FRAME frame;
		float param1;
		float param2;
		float param3;
		float param4;
		float x;
		float y;
		float z;
	};
	AP_VarS<CommandStorage> _data;
	uint16_t _seq;
	//static AP_Var_group _group;
public:
	/**
	 * Constructor for loading from memory.
	 * @param index Start at zero.
	 */
	AP_MavlinkCommand(uint16_t index) :
		_seq(index), _data(k_commands+index) {
		_data.load();
	}

	/**
	 * Constructor for saving from command a mavlink waypoint.
	 * @param cmd The mavlink_waopint_t structure for the command.
	 */
	AP_MavlinkCommand(mavlink_waypoint_t cmd) :
		_data(k_commands+cmd.seq), _seq(cmd.seq) {
		setCommand(MAV_CMD(cmd.command));
		setAutocontinue(cmd.autocontinue);
		setFrame((MAV_FRAME) cmd.frame);
		setParam1(cmd.param1);
		setParam2(cmd.param2);
		setParam3(cmd.param3);
		setParam4(cmd.param4);
		setX(cmd.x);
		setY(cmd.y);
		setZ(cmd.z);
		_data.save();
		Serial.println("============================================================");
		Serial.println("storing new command from mavlink_waypoint_t");
		Serial.print("key: "); Serial.println(_data.key(),DEC);
		Serial.print("number: "); Serial.println(cmd.seq,DEC);
		Serial.print("command: "); Serial.println(getCommand());
		Serial.print("autocontinue: "); Serial.println(getAutocontinue(),DEC);
		Serial.print("frame: "); Serial.println(getFrame(),DEC);
		Serial.print("1000*param1: "); Serial.println(int(1000*getParam1()),DEC);
		Serial.print("1000*param2: "); Serial.println(int(1000*getParam2()),DEC);
		Serial.print("1000*param3: "); Serial.println(int(1000*getParam3()),DEC);
		Serial.print("1000*param4: "); Serial.println(int(1000*getParam4()),DEC);
		Serial.print("1000*x0: "); Serial.println(int(1000*cmd.x),DEC);
		Serial.print("1000*y0: "); Serial.println(int(1000*cmd.y),DEC);
		Serial.print("1000*z0: "); Serial.println(int(1000*cmd.z),DEC);
		Serial.print("1000*x: "); Serial.println(int(1000*getX()),DEC);
		Serial.print("1000*y: "); Serial.println(int(1000*getY()),DEC);
		Serial.print("1000*z: "); Serial.println(int(1000*getZ()),DEC);
		_data.load();
		Serial.print("1000*x1: "); Serial.println(int(1000*getX()),DEC);
		Serial.print("1000*y1: "); Serial.println(int(1000*getY()),DEC);
		Serial.print("1000*z1: "); Serial.println(int(1000*getZ()),DEC);
	}
	bool save() {
		return _data.save();
	}
	bool load() {
		return _data.load();
	}
	uint8_t getSeq() {
		return _seq;
	}
	bool getAutocontinue() {
		return _data.get().autocontinue;
	}
	void setAutocontinue(bool val) {
		_data.get().autocontinue = val;
	}
	void setSeq(uint8_t val) {
		_seq = val;
	}
	MAV_CMD getCommand() {
		return _data.get().command;
	}
	void setCommand(MAV_CMD val) {
		_data.get().command = val;
	}
	MAV_FRAME getFrame() {
		return _data.get().frame;
	}
	void setFrame(MAV_FRAME val) {
		_data.get().frame = val;
	}
	float getParam1() {
		return _data.get().param1;
	}
	void setParam1(float val) {
		_data.get().param1 = val;
	}
	float getParam2() {
		return _data.get().param2;
	}
	void setParam2(float val) {
		_data.get().param2 = val;
	}
	float getParam3() {
		return _data.get().param3;
	}
	void setParam3(float val) {
		_data.get().param3 = val;
	}
	float getParam4() {
		return _data.get().param4;
	}
	void setParam4(float val) {
		_data.get().param4 = val;
	}
	float getX() {
		return _data.get().x;
	}
	void setX(float val) {
		_data.get().x = val;
	}
	float getY() {
		return _data.get().y;
	}
	void setY(float val) {
		_data.get().y = val;
	}
	float getZ() {
		return _data.get().z;
	}
	void setZ(float val) {
		_data.get().z = val;
	}
	bool getCurrent() {
		return (currentIndex.get() == getSeq());
	}
	float getLat() {
		switch (getFrame()) {
		case MAV_FRAME_GLOBAL:
		case MAV_FRAME_GLOBAL_RELATIVE_ALT:
			return getX();
			break;
		case MAV_FRAME_LOCAL:
		case MAV_FRAME_MISSION:
			return 0;
			break;
		}
	}
	void setLat(float val) {
		switch (getFrame()) {
		case MAV_FRAME_GLOBAL:
		case MAV_FRAME_GLOBAL_RELATIVE_ALT:
			setX(val);
			break;
		case MAV_FRAME_LOCAL:
		case MAV_FRAME_MISSION:
			break;
		}
	}
	float getLon() {
		switch (getFrame()) {
		case MAV_FRAME_GLOBAL:
		case MAV_FRAME_GLOBAL_RELATIVE_ALT:
			return getY();
			break;
		case MAV_FRAME_LOCAL:
		case MAV_FRAME_MISSION:
			return 0;
			break;
		}
	}
	void setLon(float val) {
		switch (getFrame()) {
		case MAV_FRAME_GLOBAL:
		case MAV_FRAME_GLOBAL_RELATIVE_ALT:
			setY(val);
			break;
		case MAV_FRAME_LOCAL:
		case MAV_FRAME_MISSION:
			break;
		}
	}
	float getAlt() {
		switch (getFrame()) {
		case MAV_FRAME_GLOBAL:
			return getZ();
			break;
		case MAV_FRAME_GLOBAL_RELATIVE_ALT:
		case MAV_FRAME_LOCAL:
			return getZ() + AP_MavlinkCommand(0).getAlt();
			break;
		case MAV_FRAME_MISSION:
			return 0;
			break;
		}
	}
	/**
	 * set the altitude in meters
	 */
	void setAlt(float val) {
		switch (getFrame()) {
		case MAV_FRAME_GLOBAL:
		case MAV_FRAME_GLOBAL_RELATIVE_ALT:
			setZ(val);
			break;
		case MAV_FRAME_LOCAL:
			setZ(val - AP_MavlinkCommand(0).getLon());
			break;
		case MAV_FRAME_MISSION:
			break;
		}
	}
	/**
	 * Get the relative altitud to home
	 * @return relative altitude in meters
	 */
	float getRelAlt() {
		switch (getFrame()) {
		case MAV_FRAME_GLOBAL:
			return getZ() - AP_MavlinkCommand(0).getAlt();
			break;
		case MAV_FRAME_GLOBAL_RELATIVE_ALT:
		case MAV_FRAME_LOCAL:
			return getZ();
			break;
		case MAV_FRAME_MISSION:
			return 0;
			break;
		}
	}
	/**
	 * set the relative altitude in meters from home
	 */
	void setRelAlt(float val) {
		switch (getFrame()) {
		case MAV_FRAME_GLOBAL:
		case MAV_FRAME_GLOBAL_RELATIVE_ALT:
			setX(val);
			break;
		case MAV_FRAME_LOCAL:
			setX(val + AP_MavlinkCommand(0).getLon());
			break;
		case MAV_FRAME_MISSION:
			break;
		}
	}

	float getRadius() {
		return getParam2();
	}

	/**
	 * conversion for outbound packets to ground station
	 * @return output the mavlink_waypoint_t packet
	 */
	mavlink_waypoint_t convert() {
		mavlink_waypoint_t mavCmd;
		mavCmd.seq = getSeq();
		mavCmd.command = getCommand();
		mavCmd.frame = getFrame();
		mavCmd.param1 = getParam1();
		mavCmd.param2 = getParam2();
		mavCmd.param3 = getParam3();
		mavCmd.param4 = getParam4();
		mavCmd.x = getX();
		mavCmd.y = getY();
		mavCmd.z = getZ();
		mavCmd.autocontinue = getAutocontinue();
		mavCmd.current = getCurrent();
		mavCmd.target_component = mavlink_system.compid;
		mavCmd.target_system = mavlink_system.sysid;
		return mavCmd;
	}
	/**
	 * Calculate the bearing from this command to the next command
	 * @param next The command to calculate the bearing to.
	 * @return the bearing
	 */
	float bearingTo(AP_MavlinkCommand next) {
		float deltaLon = getLon() - next.getLon();
		/*
		Serial.print("Lon: "); Serial.println(getLon());
		Serial.print("nextLon: "); Serial.println(next.getLon());
		Serial.print("deltaLon * 1e7: "); Serial.println(deltaLon*1e7);
		*/
		return atan2(sin(deltaLon)*cos(next.getLat()),
				cos(getLat())*sin(next.getLat()) -
				sin(getLat())*cos(next.getLat())*cos(deltaLon));
	}

	/**
	 * Bearing form this command to a gps coordinate in integer units
	 * @param lat latitude in degrees E-7
	 * @param lon longitude in degrees E-7
	 * @return
	 */
	float bearingTo(int32_t lat, int32_t lon) {
		// have to be careful to maintain the precision of the gps coordinate
		float deltaLon = (lon - rad2DegInt*getLon())/rad2DegInt;
		float nextLat = lat/rad2DegInt;
		return atan2(sin(deltaLon)*cos(nextLat), cos(getLat())*sin(nextLat) -
				sin(getLat())*cos(nextLat)*cos(deltaLon));
	}

	/**
	 * Distance to another command
	 * @param next The command to measure to.
	 * @return The distance in meters.
	 */
	float distanceTo(AP_MavlinkCommand next) {
		float sinDeltaLat2 = sin((getLat()-next.getLat())/2);
		float sinDeltaLon2 = sin((getLon()-next.getLon())/2);
		float a = sinDeltaLat2*sinDeltaLat2 + cos(getLat())*cos(next.getLat())*
				sinDeltaLon2*sinDeltaLon2;
		float c = 2*atan2(sqrt(a),sqrt(1-a));
		return rEarth*c;
	}

	/**
	 * Distance to a gps coordinate in integer units
	 * @param lat latitude in degrees E-7
	 * @param lon longitude in degrees E-7
	 * @return The distance in meters.
	 */
	float distanceTo(int32_t lat, int32_t lon) {
		float sinDeltaLat2 = sin((lat - rad2DegInt*getLat())/rad2DegInt/2);
		float sinDeltaLon2 = sin((lon - rad2DegInt*getLon())/rad2DegInt/2);
		float a = sinDeltaLat2*sinDeltaLat2 +
				cos(getLat())*cos(lat/rad2DegInt)*sinDeltaLon2*sinDeltaLon2;
		float c = 2*atan2(sqrt(a),sqrt(1-a));
		return rEarth*c;
	}

	static AP_Uint8 number;
	static AP_Uint8 currentIndex;
	// constants
	static float rEarth;
	static float rad2Deg;
	static float rad2DegInt;
};
AP_Uint8 AP_MavlinkCommand::number = 1;
AP_Uint8 AP_MavlinkCommand::currentIndex = 1;
float AP_MavlinkCommand::rad2DegInt = 1e7*rad2Deg;
float AP_MavlinkCommand::rad2Deg = 180/M_PI;
float AP_MavlinkCommand::rEarth = 6371000;


#endif /* AP_MAVLINKCOMMAND_H_ */
