using System.Collections.Generic;

namespace SB_Services.DTOs
{
    public class CreateGroupRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        // Danh sách nickname của các thành viên được thêm ban đầu
        public List<string> Members { get; set; } = new List<string>();
    }
}
