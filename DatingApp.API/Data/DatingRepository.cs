using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatingApp.API.Models;
using DatingApp.API.Helpers;
using Microsoft.EntityFrameworkCore;
using System;

namespace DatingApp.API.Data
{
    public class DatingRepository : IDatingRepository
    {
        private readonly DataContext _context;

        public DatingRepository(DataContext context)
        {
            _context = context;
        }
        public void Add<T>(T entity) where T : class
        {
            _context.Add(entity);
        }

        public void Delete<T>(T entity) where T : class
        {
            _context.Remove(entity);
        }

        public async Task<Like> GetLike(int userId, int recipientId)
        {
            return await _context.Likes.FirstOrDefaultAsync(p => 
                p.LikerId == userId && p.LikeeId == recipientId);
        }

        public async Task<Photo> GetMainPhotoForUser(int userId)
        {
            return await _context.Photos
                .Where(u => u.UserId == userId)
                .FirstOrDefaultAsync(p => p.IsMain);
        }

        public async Task<Photo> GetPhoto(int id)
        {
            var photo = await _context.Photos
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == id);

            return photo;
        }

        public async Task<PagedList<Photo>> GetPhotosForModeration(PaginationParams paginationParams)
        {
            var photos = _context.Photos
                .Include(p => p.User)
                .IgnoreQueryFilters()
                .Where(p => !p.IsApproved)
                .AsQueryable();

            return await PagedList<Photo>.CreateAsync(photos, 
                paginationParams.PageNumber, paginationParams.PageSize);
        }

        public async Task<User> GetUser(int id, bool isCurrentUser)
        {
            var query = _context.Users
                .Include(p => p.Photos)
                .AsQueryable();

            if(isCurrentUser)
                query = query.IgnoreQueryFilters();

            var user = await query.FirstOrDefaultAsync(p => p.Id == id);

            return user;
        }
        public async Task<PagedList<User>> GetUsers(UserParams userParams)
        {
            var users = _context.Users
                .Include(p => p.Photos)
                .OrderByDescending(u => u.LastActive)
                .AsQueryable();

            users = users.Where(p => p.Id != userParams.UserId);
            users = users.Where(p => p.Gender == userParams.Gender);

            if(userParams.Likers)
            {
                var userLikers = await GetUserLikes(userParams.UserId, userParams.Likers);
                users = users.Where(u => userLikers.Contains(u.Id));
            }
            
            if(userParams.Likees)
            {
                var userLikees = await GetUserLikes(userParams.UserId, userParams.Likers);
                users = users.Where(u => userLikees.Contains(u.Id));
            }

            if(userParams.MinAge != 18 || userParams.MaxAge != 99)
            {
                var minDob = DateTime.Today.AddYears(-userParams.MaxAge -1);
                var maxDoB = DateTime.Today.AddYears(-userParams.MinAge);

                users = users.Where(p => p.DateOfBirth >=minDob && p.DateOfBirth <=maxDoB);
            }

            if (!string.IsNullOrEmpty(userParams.OrderBy))
            {
                switch (userParams.OrderBy)
                {
                    case "created":
                        users = users.OrderByDescending(u => u.Created);
                        break;
                    default:
                        users = users.OrderByDescending(u => u.LastActive);
                        break;
                }
            }
            return await PagedList<User>.CreateAsync(users, userParams.PageNumber, userParams.PageSize);
        }

        private async Task<IEnumerable<int>> GetUserLikes(int id, bool likers)
        {
            var user = await _context.Users.Include(x => x.Likers)
            .Include(x => x.Likees)
            .FirstOrDefaultAsync(u => u.Id == id);

            if(likers)
            {
                return user.Likers.Where(u => u.LikeeId == id).Select(s => s.LikerId);
            }
            else
            {
                return user.Likees.Where(u => u.LikerId == id).Select(s => s.LikeeId);
            }
        }

        public async Task<bool> SaveAll()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<Message> GetMessage(int id)
        {
            return await _context.Messages.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<PagedList<Message>> GetMessagesForUser(MessageParams messageParams)
        {
            var messages = _context.Messages
                .Include(p => p.Sender)
                .ThenInclude(p => p.Photos)
                .Include(p => p.Recipient)
                .ThenInclude(p => p.Photos)
                .AsQueryable();

            switch (messageParams.MessageContainer)
            {
                case "Inbox":
                    messages = messages.Where(p => p.RecipientId == messageParams.UserId
                        && !p.RecipientDeleted);
                    break;
                case "Outbox":
                    messages = messages.Where(p => p.SenderId == messageParams.UserId
                        && !p.SenderDeleted);
                    break;
                default:
                    messages = messages.Where(p => p.RecipientId == messageParams.UserId 
                        && !p.IsRead && !p.RecipientDeleted);
                    break;
            }

            messages = messages.OrderByDescending(p => p.MessageSent);

            return await PagedList<Message>.CreateAsync(messages,
                messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<Message>> GetMessageThread(int userId, int recipientId)
        {
            var messages = await _context.Messages
                .Include(p => p.Sender).ThenInclude(p => p.Photos)
                .Include(p => p.Recipient).ThenInclude(p => p.Photos)
                .Where(p => p.RecipientId == userId && !p.RecipientDeleted && p.SenderId == recipientId
                    || p.RecipientId == recipientId && !p.RecipientDeleted && p.SenderId == userId)
                .OrderByDescending(p => p.MessageSent)
                .ToListAsync();

            return messages;
        }
    }
}