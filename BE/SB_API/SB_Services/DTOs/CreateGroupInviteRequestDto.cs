namespace SB_Services.DTOs
{
    public class CreateGroupInviteRequestDto
    {
        public int? ExpiresInHours { get; set; }
        public int? MaxUses { get; set; }
    }
}
