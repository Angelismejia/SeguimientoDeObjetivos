using Application.DTOs.Users;
using Application.Interfaces.Services;

namespace Application.Services
{
    public class UserDataExportService : IUserDataExportService
    {
        private readonly IUserService _userService;
        private readonly ICategoryService _categoryService;
        private readonly IObjectiveService _objectiveService;
        private readonly ITaskService _taskService;
        private readonly IDiaryEntryService _diaryEntryService;
        private readonly IBadgeService _badgeService;

        public UserDataExportService(
            IUserService userService,
            ICategoryService categoryService,
            IObjectiveService objectiveService,
            ITaskService taskService,
            IDiaryEntryService diaryEntryService,
            IBadgeService badgeService)
        {
            _userService = userService;
            _categoryService = categoryService;
            _objectiveService = objectiveService;
            _taskService = taskService;
            _diaryEntryService = diaryEntryService;
            _badgeService = badgeService;
        }

        public async Task<UserDataExportDto> ExportAsync(int userId)
        {
            var user = await _userService.GetByIdAsync(userId);

            return new UserDataExportDto
            {
                User = user,
                Categories = await _categoryService.GetByUserIdAsync(userId),
                Objectives = await _objectiveService.GetByUserIdAsync(userId),
                Tasks = await _taskService.GetByUserIdAsync(userId),
                DiaryEntries = await _diaryEntryService.GetByUserIdAsync(userId),
                Badges = await _badgeService.GetByUserIdAsync(userId)
            };
        }
    }
}
