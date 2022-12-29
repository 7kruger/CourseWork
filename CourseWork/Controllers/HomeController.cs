using CourseWork.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CourseWork.Models.Entities;
using System.Text.Json;

namespace CourseWork.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext db;
        public HomeController(ApplicationDbContext context)
        {
            db = context;
        }

        public async Task<ActionResult> Index()
        {
            var collections = await db.Collections.ToListAsync();
            return View(collections.OrderByDescending(c => c.Date).ToList());
        }

        public async Task<ActionResult> Search(string value)
        {
            List<Collection> result;
            result = await db.Collections.Where(c => c.Name.Contains(value) || c.Description.Contains(value) || c.Author.Contains(value)).ToListAsync();
			return View(result);
        }

        public async Task<ActionResult> Collection(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }
            return View(await db.Collections.Include(i => i.Items).FirstOrDefaultAsync(c => c.Id == id));
        }

        public async Task<ActionResult> Item(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }
            var item = await db.Items.FirstOrDefaultAsync(i => i.Id == id);
            if (!string.IsNullOrWhiteSpace(item.CollectionId))
            {
                ViewData["CollectionName"] = db.Collections.FirstOrDefault(c => c.Id == item.CollectionId).Name;
            }
            return View(item);
        }

        public async Task<ActionResult> LoadComments(string collectionId)
        {
            var comments = await db.Comments.Where(c => c.CollectionId == collectionId).ToListAsync();
            return Ok(comments.OrderByDescending(c => c.Date));
        }
                
        public async Task<ActionResult> LoadItemComments(string itemId)
        {
            var comments = await db.Comments.Where(c => c.ItemId == itemId).ToListAsync();
            return Ok(comments.OrderByDescending(c => c.Date));
        }

        public async Task<ActionResult> LoadLikesInfo(string collectionId)
        {
            var currentUserLikedInfo = await db.Likes.FirstOrDefaultAsync(l => l.CollectionId == collectionId && l.UserName == GetCurrentUser());
            var liked = currentUserLikedInfo == null ? false : true;
            string json = "";
            var count = db.Likes.Where(l => l.CollectionId == collectionId).Count();
            json = JsonSerializer.Serialize(new
            {
                likesCount = count,
                liked = liked
            });

            return Ok(json);
        }

        public async Task<ActionResult> LoadItemLikesInfo(string imtemId)
        {
            var currentUserLikedInfo = await db.Likes.FirstOrDefaultAsync(l => l.ItemId == imtemId && l.UserName == GetCurrentUser());
            var liked = currentUserLikedInfo == null ? false : true;
            string json = "";
            var count = db.Likes.Where(l => l.ItemId == imtemId).Count();
            json = JsonSerializer.Serialize(new
            {
                likesCount = count,
                liked = liked
            });

            return Ok(json);
        }

        [HttpPost]
        public async Task<ActionResult> SetLike(string collectionId)
        {
            var like = new Like
            {
                CollectionId = collectionId,
                UserName = GetCurrentUser()
            };
            await db.Likes.AddAsync(like);
            await db.SaveChangesAsync();

            return Ok();
        }
        [HttpPost]
        public async Task<ActionResult> SetItemLike(string itemId)
        {
            var like = new Like
            {
                ItemId = itemId,
                UserName = GetCurrentUser()
            };
            await db.Likes.AddAsync(like);
            await db.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<ActionResult> RemoveLike(string collectionId)
        {
            var like = await db.Likes.FirstOrDefaultAsync(l => l.CollectionId == collectionId && l.UserName == GetCurrentUser());
            db.Likes.Remove(like);
            await db.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<ActionResult> RemoveItemLike(string itemId)
        {
            var like = await db.Likes.FirstOrDefaultAsync(l => l.ItemId == itemId && l.UserName == GetCurrentUser());
            db.Likes.Remove(like);
            await db.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<ActionResult> AddComment(string collectionId, string content)
        {
            Comment comment = new Comment
            {
                CollectionId = collectionId,
                UserName = GetCurrentUser(),
                Content = content,
                Date = DateTime.Now
            };

            await db.Comments.AddAsync(comment);
            await db.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<ActionResult> AddItemComment(string itemId, string content)
        {
            Comment comment = new Comment
            {
                ItemId = itemId,
                UserName = GetCurrentUser(),
                Content = content,
                Date = DateTime.Now
            };

            await db.Comments.AddAsync(comment);
            await db.SaveChangesAsync();

            return Ok();
        }
        private string GetCurrentUser() => User.Identity.Name;
    }
}
