using System;
using System.Collections.Generic;
using System.Text;

namespace Photobooth.Model
{
    internal class PortraitSnapshot
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime TakenAt { get; set; }
        public uint ClassJobId { get; set; }

        public string ClassJobName { get; set; }

        public string SerializedSnapshot { get; set;  }

        public PortraitSnapshot(uint classJobId, string classJobName, string serializedSnapshot)
        {
            TakenAt = DateTime.Now;
            ClassJobId = classJobId;
            ClassJobName = classJobName;
            SerializedSnapshot = serializedSnapshot;
        }
    }
}
