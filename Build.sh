#!/bin/bash
xed /home/martin/RiderProjects/WhereCanIGo/GameData/WhereCanIGo/Changelog.cfg
xed /home/martin/RiderProjects/WhereCanIGo/GameData/WhereCanIGo/WhereCanIGo.version
zip -r WhereCanIGo.zip GameData
notify-send "WhereCanIGo Release Build Finished"
