using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Migrations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServerCore.DataModel;
using ServerCore.Helpers;
using static ServerCore.Helpers.StandingsHelper;

namespace ServerCore.Areas.Identity.Pages.Account.Manage
{
    public class PastEventsModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly PuzzleServerContext _context;

        public PastEventsModel(
            UserManager<IdentityUser> userManager,
            PuzzleServerContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        /// <summary>
        /// The list of details about past events that the user participated in.
        /// </summary>
        public List<PastEventDetails> PastEventDetailsList { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            PuzzleUser puzzleUser = await PuzzleUser.GetPuzzleUser(user.Id, _context);

            // Get all previous events along with what team the the player was in
            PastEventDetailsList = await (from player in _context.PlayerInEvent
                                                    where player.PlayerId == puzzleUser.ID
                                                    join teamMember in _context.TeamMembers
                                                        on new { PlayerId = player.PlayerId, EventId = player.EventId } equals
                                                        new { PlayerId = teamMember.Member.ID, EventId = teamMember.Team.EventID }
                                                    orderby player.Event.EventBegin
                                                    select new PastEventDetails
                                                    {
                                                        Event = player.Event,
                                                        Team = teamMember.Team
                                                    }).ToListAsync();

            // Populate rest of event details fields
            foreach(PastEventDetails eventDetails in PastEventDetailsList)
            {
                List<TeamStats> teamsStats = await StandingsHelper.GetStandingsOrderedByScore(_context, eventDetails.Event, hideDisqualifiedTeams: false);
                TeamStats playerTeamStat = null;
                int leaderboardPlacement = 0;
                for (int i = 0; i < teamsStats.Count; ++i)
                {
                    if (teamsStats[i].Team.ID == eventDetails.Team.ID)
                    {
                        playerTeamStat = teamsStats[i];
                        leaderboardPlacement = i + 1;
                        break;
                    }
                }

                if (playerTeamStat == null)
                {
                    eventDetails.Status = Status.Unknown;
                }
                else if (eventDetails.Team.IsDisqualified)
                {
                    eventDetails.Status = Status.Disqualified;
                }
                else if (eventDetails.Event.AnswerSubmissionEnd >= DateTime.UtcNow)
                {
                    eventDetails.Status = Status.InProgress;
                }
                else
                {
                    eventDetails.Status = Status.Completed;
                    eventDetails.LeaderboardPlacement = leaderboardPlacement;
                    eventDetails.TotalTeamsOnLeaderboard = teamsStats.Count;
                }
            }

            return Page();
        }

        public class PastEventDetails
        {
            /// <summary>
            /// The event.
            /// </summary>
            public Event Event { get; set; }

            /// <summary>
            /// Gets or sets the team.
            /// </summary>
            public Team Team { get; set; }

            /// <summary>
            /// Gets the user's status in the event.
            /// </summary>
            public Status Status { get; set; }

            /// <summary>
            /// The leaderboard placement.
            /// </summary>
            public int LeaderboardPlacement { get; set; }

            /// <summary>
            /// The total teams on the leaderboard.
            /// </summary>
            public int TotalTeamsOnLeaderboard { get; set; }
        }

        public enum Status
        {
            /// <summary>
            /// Unknown status.
            /// </summary>
            Unknown,

            /// <summary>
            /// The event is still ongoing.
            /// </summary>
            InProgress,

            /// <summary>
            /// The event is completed.
            /// </summary>
            Completed,

            /// <summary>
            /// The user had been disqualified from the event.
            /// </summary>
            Disqualified
        }
    }
}
