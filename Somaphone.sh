#!/bin/bash

#enable bluetooth
/usr/sbin/rfkill unblock bluetooth

#start our program as a daemon
/home/root/10VE/bin/Somaphone --osc 192.168.0.5 --port 4444 --rate 100 --name Antony



