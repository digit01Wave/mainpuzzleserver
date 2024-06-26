﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerCore.DataModel;
using ServerCore.ModelBases;

namespace ServerCore.Pages.Responses
{
    [Authorize(Policy = "IsEventAdminOrAuthorOfPuzzle")]
    public class CreateModel : EventSpecificPageModel
    {
        public CreateModel(PuzzleServerContext serverContext, UserManager<IdentityUser> userManager) : base(serverContext, userManager)
        {
        }

        [BindProperty]
        public Response PuzzleResponse { get; set; }

        public int PuzzleId { get; set; }

        public Puzzle Puzzle { get; set; }

        public async Task<IActionResult> OnGetAsync(int puzzleId)
        {
            PuzzleId = puzzleId;
            Puzzle = await _context.Puzzles.Where(m => m.ID == puzzleId).FirstOrDefaultAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int puzzleId)
        {
            if (!ModelState.IsValid)
            {
                return await OnGetAsync(puzzleId);
            }

            PuzzleResponse.PuzzleID = puzzleId;

            if (PuzzleResponse.ResponseText != null && PuzzleResponse.ResponseText.StartsWith("Html.Raw("))
            {
                ModelState.AddModelError("PuzzleResponse.ResponseText", "Reponses using \"Html.Raw\" must start with a plain text version.");
                return await OnGetAsync(puzzleId);
            }

            // Ensure that the response text is unique across all responses for this puzzle.
            bool duplicateResponse = await (from Response r in _context.Responses
                                            where r.PuzzleID == puzzleId &&
                                            r.SubmittedText == PuzzleResponse.SubmittedText
                                            select r).AnyAsync();
            if (duplicateResponse)
            {
                ModelState.AddModelError("PuzzleResponse.SubmittedText", "Submission text is not unique");
                return await OnGetAsync(puzzleId);
            }

            _context.Responses.Add(PuzzleResponse);
            await _context.SaveChangesAsync();

            await PuzzleStateHelper.UpdateTeamsWhoSentResponse(_context, PuzzleResponse);

            return RedirectToPage("./Index", new { puzzleid = puzzleId });
        }
    }
}