﻿using System;
using System.Collections.Generic;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.Models
{
    public class LatestPoint
    {
        public Stage Stage { get; set; }
        public Level Level { get; set; }
        public long Time { get; set; }
        public int Points { get; set; }
        public IReadOnlyDictionary<Player, DateTime> Occurences { get; set; }
    }
}
