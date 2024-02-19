using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ServerCore.DataModel;

namespace ServerCore.Helpers
{
    public static class StandingsHelper
    {
        public static async Task<List<TeamStats>> GetStandingsOrderedByScore(
            PuzzleServerContext context,
            Event e,
            bool hideDisqualifiedTeams)
        {
            var puzzleData = await context.Puzzles
                .Where(p => p.Event == e)
                .ToDictionaryAsync(p => p.ID, p => new { p.SolveValue, p.IsCheatCode, p.IsPuzzle, p.IsFinalPuzzle });

            int finalMetaCount = puzzleData.Where(p => p.Value.IsFinalPuzzle).Count();

            DateTime submissionEnd = e.AnswerSubmissionEnd;
            var stateData = await PuzzleStateHelper.GetSparseQuery(context, e, puzzle: null, team: null)
                .Where(pspt => pspt.SolvedTime != null && pspt.SolvedTime <= submissionEnd)
                .Select(pspt => new { pspt.PuzzleID, pspt.TeamID, pspt.SolvedTime })
                .ToListAsync();

            // Hide disqualified teams from the standings page if desired.
            var teams = await context.Teams
                .Where(t => t.Event == e 
                    && (!hideDisqualifiedTeams || t.IsDisqualified == false))
                .ToListAsync();

            Dictionary<int, TeamStats> teamStats = new Dictionary<int, TeamStats>(teams.Count);
            foreach (var t in teams)
            {
                teamStats[t.ID] = new TeamStats { Team = t, FinalMetaSolveTime = DateTime.MaxValue };
            }

            foreach (var s in stateData)
            {
                if (!puzzleData.TryGetValue(s.PuzzleID, out var p) || !teamStats.TryGetValue(s.TeamID, out var ts))
                {
                    continue;
                }

                ts.Score += p.SolveValue;
                if (p.IsPuzzle)
                {
                    ts.SolveCount++;
                }
                if (p.IsCheatCode)
                {
                    ts.Cheated = true;
                    ts.FinalMetaSolveTime = DateTime.MaxValue;
                }
                if (p.IsFinalPuzzle && !ts.Cheated)
                {
                    if (ts.LatestFinalMetaSolveTime < s.SolvedTime.Value)
                    {
                        ts.LatestFinalMetaSolveTime = s.SolvedTime.Value;
                    }
                    ts.FinalMetaSolveCount++;

                    if (ts.FinalMetaSolveCount == finalMetaCount)
                    {
                        ts.FinalMetaSolveTime = ts.LatestFinalMetaSolveTime;
                    }
                }
            }

            List<TeamStats> teamsFinal = teamStats.Values.OrderBy(t => t.FinalMetaSolveTime)
                .ThenByDescending(t => t.Score)
                .ThenByDescending(t => t.SolveCount)
                .ThenBy(t => t.Team.Name)
                .ToList();

            TeamStats prevStats = null;
            for (int i = 0; i < teamsFinal.Count; i++)
            {
                var stats = teamsFinal[i];

                if (prevStats == null || stats.FinalMetaSolveTime != prevStats.FinalMetaSolveTime || stats.Score != prevStats.Score)
                {
                    stats.Rank = i + 1;
                }
                else
                {
                    stats.Rank = prevStats.Rank;
                }

                prevStats = stats;
            }

            return teamsFinal;
        }

        public class TeamStats
        {
            public Team Team;
            public bool Cheated;
            public int SolveCount;
            public int FinalMetaSolveCount;
            public int Score;
            public int? Rank;
            public DateTime LatestFinalMetaSolveTime = DateTime.MinValue;
            public DateTime FinalMetaSolveTime = DateTime.MaxValue;
        }
    }
}
