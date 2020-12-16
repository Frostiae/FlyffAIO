using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorApp
{
    public class Player
    {
        public int m_str { get; set; }
        public int m_dex { get; set; }
        public int m_sta { get; set; }
        public int m_int { get; set; }


        public double GetAsalDamage()
        {
            double result = 0;
            int skillLevelTemp = 10;
            int level = 120;
            int MP = (int)(((((level * 2.0f) + (m_int * 8.0f)) * 1) + 22.0f) + (m_int * 1));

            result = ((m_str / 10) * skillLevelTemp) * (5 + MP / 10) + 150; // This is assuming the skill level is 10


            return result;
        }
    }

}
