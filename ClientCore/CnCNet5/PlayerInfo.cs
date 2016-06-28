﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ClientCore.CnCNet5
{
    public class PlayerInfo
    {
        public PlayerInfo() { }

        public PlayerInfo(string name)
        {
            Name = name;
        }

        public PlayerInfo(string name, int sideId, int startingLocation, int colorId, int teamId)
        {
            Name = name;
            SideId = sideId;
            StartingLocation = startingLocation;
            ColorId = colorId;
            TeamId = teamId;
        }

        public string Name { get; set; }
        public int SideId { get; set; }
        public int StartingLocation { get; set; }
        public int ColorId { get; set; }
        public int TeamId { get; set; }
        public bool Ready { get; set; }
        public bool IsAI { get; set; }
        public bool IsInGame { get; set; }
        string ipAddress = "0.0.0.0";
        public string IPAddress { get { return ipAddress; } set { ipAddress = value; } }
        public int Port { get; set; }
        public int ForcedColor { get; set; }
        public bool Verified { get; set; }
    }
}