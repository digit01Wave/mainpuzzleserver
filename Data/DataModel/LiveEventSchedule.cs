﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace ServerCore.DataModel
{
    public class LiveEventSchedule
    {
        /// <summary>
        /// The ID
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public int LiveEventId { get; set; }

        [Required]
        public int TeamId { get; set; }

        [Required]
        public virtual Team Team { get; set; }

        [Required]
        public DateTime StartTimeUtc { get; set; }

        [Required]
        public virtual LiveEvent LiveEvent { get; set; }

        public DateTime LastNotifiedUtc { get; set; } = DateTime.MinValue;

        public LiveEventSchedule()
        {
        }

        public LiveEventSchedule (LiveEvent liveEvent, Team team, DateTime slotTimeUtc)
        {
            LiveEvent = liveEvent;
            LiveEventId = liveEvent.ID;
            Team = team;
            TeamId = team.ID;
            StartTimeUtc = slotTimeUtc;
        }
    }
}
