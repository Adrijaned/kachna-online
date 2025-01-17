// LeaderboardItemDto.cs
// Author: Ondřej Ondryáš

using KachnaOnline.Dto.Swagger;

namespace KachnaOnline.Dto.ClubInfo
{
    /// <summary>
    /// Represents a pair of user nickname and the amount of prestige points they have gained.
    /// </summary>
    public class LeaderboardItemDto
    {
        /// <summary>
        /// A person's nickname (or their initials).
        /// </summary>
        [SwaggerNotNull]
        public string Name { get; set; }

        /// <summary>
        /// The amount of a person's prestige points.
        /// </summary>
        public int Prestige { get; set; }
    }
}
