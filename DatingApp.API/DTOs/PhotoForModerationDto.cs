using System;

namespace DatingApp.API.DTOs
{
    public class PhotoForModerationDto
    {
        public int Id { get; set; }
        public DateTime DateAdded { get; set; }
        public string UserKnownAs { get; set; }
        public string Url { get; set; }
        public string PublicId { get; set; }
    }
}