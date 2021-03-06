﻿using System;

namespace Horton.Scripts
{
    public class MigrationScript : ScriptFile, IComparable<MigrationScript>
    {
        public MigrationScript(string filePath, string fileName, int serialNumber)
            : base(filePath, fileName)
        {
            SerialNumber = serialNumber;
        }

        public int SerialNumber { get; }

        public override byte TypeCode => 1;

        public override bool ConflictOnContent => true;

        public override int CompareTo(ScriptFile other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (other is MigrationScript otherMigrationScript)
            {
                return CompareTo(otherMigrationScript);
            }

            if (other is ObjectScript)
            {
                return -1;
            }

            return -1;
        }

        public int CompareTo(MigrationScript other) => SerialNumber.CompareTo(other.SerialNumber);
    }
}