using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServerCore.DataModel;
using ServerCore.Helpers;
using ServerCore.ModelBases;
using static ServerCore.Helpers.StandingsHelper;

namespace ServerCore.Pages.Events
{
    [Authorize(Policy = "IsRegisteredForEvent")]
    public class StandingsModel : EventSpecificPageModel
    {
        public List<TeamStats> Teams { get; private set; }

        public SortOrder? Sort { get; set; }

        private const SortOrder DefaultSort = SortOrder.RankAscending;

        public StandingsModel(PuzzleServerContext serverContext, UserManager<IdentityUser> userManager) : base(serverContext, userManager)
        {
        }

        public async Task OnGetAsync(SortOrder? sort)
        {
            Sort = sort;
            List<TeamStats> teamsFinal = await StandingsHelper.GetStandingsOrderedByScore(_context, this.Event, hideDisqualifiedTeams: true);

            switch(sort)
            {
                case SortOrder.RankAscending:
                    break;
                case SortOrder.RankDescending:
                    teamsFinal.Reverse();
                    break;
                case SortOrder.NameAscending:
                    teamsFinal = teamsFinal.OrderBy(ts => ts.Team.Name).ToList();
                    break;
                case SortOrder.NameDescending:
                    teamsFinal = teamsFinal.OrderByDescending(ts => ts.Team.Name).ToList();
                    break;
                case SortOrder.PuzzlesAscending:
                    teamsFinal = teamsFinal.OrderBy(ts => ts.SolveCount).ThenByDescending(ts => ts.Rank).ThenByDescending(ts => ts.Team.Name).ToList();
                    break;
                case SortOrder.PuzzlesDescending:
                    teamsFinal = teamsFinal.OrderByDescending(ts => ts.SolveCount).ThenBy(ts => ts.Rank).ThenBy(ts => ts.Team.Name).ToList();
                    break;
                case SortOrder.ScoreAscending:
                    teamsFinal = teamsFinal.OrderBy(ts => ts.Score).ThenByDescending(ts => ts.Rank).ThenByDescending(ts => ts.Team.Name).ToList();
                    break;
                case SortOrder.ScoreDescending:
                    teamsFinal = teamsFinal.OrderByDescending(ts => ts.Score).ThenBy(ts => ts.Rank).ThenBy(ts => ts.Team.Name).ToList();
                    break;
                case SortOrder.HintsEarnedAscending:
                    teamsFinal = teamsFinal.OrderBy(ts => ts.Team.HintCoinsEarned).ThenByDescending(ts => ts.Rank).ThenByDescending(ts => ts.Team.Name).ToList();
                    break;
                case SortOrder.HintsEarnedDescending:
                    teamsFinal = teamsFinal.OrderByDescending(ts => ts.Team.HintCoinsEarned).ThenBy(ts => ts.Rank).ThenBy(ts => ts.Team.Name).ToList();
                    break;
                case SortOrder.HintsUsedAscending:
                    teamsFinal = teamsFinal.OrderBy(ts => ts.Team.HintCoinsUsed).ThenByDescending(ts => ts.Rank).ThenByDescending(ts => ts.Team.Name).ToList();
                    break;
                case SortOrder.HintsUsedDescending:
                    teamsFinal = teamsFinal.OrderByDescending(ts => ts.Team.HintCoinsUsed).ThenBy(ts => ts.Rank).ThenBy(ts => ts.Team.Name).ToList();
                    break;
            }

            this.Teams = teamsFinal;
        }

        /// <summary>
        /// Set up the proper sort for the link in a column header.
        /// </summary>
        /// <param name="standardSort">The sort you'll get for the first click on that column header</param>
        /// <param name="reverseSort">The sort you'll get for the second click on that column header</param>
        /// <returns></returns>
        public SortOrder? SortForColumnLink(SortOrder standardSort, SortOrder reverseSort)
        {
            SortOrder result = standardSort;

            if (result == (this.Sort ?? DefaultSort))
            {
                result = reverseSort;
            }

            if (result == DefaultSort)
            {
                return null;
            }

            return result;
        }

        public enum SortOrder
        {
            RankAscending,
            RankDescending,
            NameAscending,
            NameDescending,
            PuzzlesAscending,
            PuzzlesDescending,
            ScoreAscending,
            ScoreDescending,
            HintsEarnedAscending,
            HintsEarnedDescending,
            HintsUsedAscending,
            HintsUsedDescending
        }
    }
}
