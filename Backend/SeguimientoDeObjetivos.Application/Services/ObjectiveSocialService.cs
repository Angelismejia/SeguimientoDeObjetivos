using Application.DTOs.Notifications;
using Application.DTOs.Objectives;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

namespace Application.Services
{
    public class ObjectiveSocialService : IObjectiveSocialService
    {
        private const int LargoDelExtracto = 80;

        private readonly IObjectiveSocialRepository _socialRepository;
        private readonly IObjectiveRepository _objectiveRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;

        public ObjectiveSocialService(
            IObjectiveSocialRepository socialRepository,
            IObjectiveRepository objectiveRepository,
            IUserRepository userRepository,
            INotificationService notificationService,
            IUnitOfWork unitOfWork)
        {
            _socialRepository = socialRepository;
            _objectiveRepository = objectiveRepository;
            _userRepository = userRepository;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<ObjectiveSocialDto>> GetSummariesAsync(int requesterId, IEnumerable<int> objectiveIds)
        {
            var ids = objectiveIds.Distinct().ToList();
            if (ids.Count == 0) return Enumerable.Empty<ObjectiveSocialDto>();

            var likes = (await _socialRepository.GetLikesAsync(ids)).ToList();
            var comentarios = await _socialRepository.CountCommentsAsync(ids);

            return ids.Select(id => new ObjectiveSocialDto
            {
                ObjectiveId = id,
                LikeCount = likes.Count(l => l.ObjectiveId == id),
                LikedByMe = likes.Any(l => l.ObjectiveId == id && l.UserId == requesterId),
                CommentCount = comentarios.TryGetValue(id, out var n) ? n : 0
            }).ToList();
        }

        // Idempotente: dar like dos veces o quitar uno que no estaba no es un error,
        // asi un doble click no le muestra nada raro al usuario.
        public async Task<ObjectiveSocialDto> SetLikeAsync(int requesterId, int objectiveId, bool liked)
        {
            var objective = await ObjetivoLogradoAsync(objectiveId);
            var existente = await _socialRepository.GetLikeAsync(objectiveId, requesterId);

            if (liked && existente is null)
            {
                _socialRepository.AddLike(new ObjectiveLike { ObjectiveId = objectiveId, UserId = requesterId });
                await _unitOfWork.SaveChangesAsync();
                await AvisarAlDuenioAsync(objective, requesterId, nombre => new CreateNotificationDto
                {
                    UserId = objective.UserId,
                    Title = "¡Le gustó tu logro!",
                    Message = $"A {nombre} le gustó que cumpliste \"{objective.Title}\".",
                    Type = "objective_liked"
                });
            }
            else if (!liked && existente is not null)
            {
                _socialRepository.RemoveLike(existente);
                await _unitOfWork.SaveChangesAsync();
            }

            return (await GetSummariesAsync(requesterId, new[] { objectiveId })).Single();
        }

        public async Task<IEnumerable<ObjectiveCommentDto>> GetCommentsAsync(int objectiveId)
        {
            var comentarios = await _socialRepository.GetCommentsAsync(objectiveId);
            return comentarios.Select(ToDto);
        }

        public async Task<ObjectiveCommentDto> GetCommentAsync(int commentId)
        {
            var comentario = await _socialRepository.GetCommentAsync(commentId);
            if (comentario is null) throw new NotFoundException("ObjectiveComment", commentId);
            return ToDto(comentario);
        }

        public async Task<ObjectiveCommentDto> AddCommentAsync(int requesterId, int objectiveId, CreateObjectiveCommentDto dto)
        {
            var contenido = (dto.Content ?? string.Empty).Trim();
            if (contenido.Length == 0)
                throw new BusinessRuleException("El comentario no puede estar vacío.");

            var objective = await ObjetivoLogradoAsync(objectiveId);
            var autor = await _userRepository.GetByIdAsync(requesterId);
            if (autor is null) throw new NotFoundException("User", requesterId);

            var comentario = new ObjectiveComment
            {
                ObjectiveId = objectiveId,
                UserId = requesterId,
                Content = contenido,
                User = autor
            };
            _socialRepository.AddComment(comentario);
            await _unitOfWork.SaveChangesAsync();

            await AvisarAlDuenioAsync(objective, requesterId, nombre => new CreateNotificationDto
            {
                UserId = objective.UserId,
                Title = "Nuevo comentario",
                Message = $"{nombre} comentó tu logro \"{objective.Title}\": \"{Extracto(contenido)}\"",
                Type = "objective_comment"
            });

            return ToDto(comentario);
        }

        public async Task DeleteCommentAsync(int commentId)
        {
            var comentario = await _socialRepository.GetCommentAsync(commentId);
            if (comentario is null) throw new NotFoundException("ObjectiveComment", commentId);
            _socialRepository.RemoveComment(comentario);
            await _unitOfWork.SaveChangesAsync();
        }

        // La comunidad es para celebrar lo que los amigos ya lograron: los objetivos
        // en marcha se ven, pero no se les da like ni se comentan.
        private async Task<Objective> ObjetivoLogradoAsync(int objectiveId)
        {
            var objective = await _objectiveRepository.GetByIdAsync(objectiveId);
            if (objective is null) throw new NotFoundException("Objective", objectiveId);
            if (objective.Status != ObjectiveStatus.Completed)
                throw new BusinessRuleException("Solo se puede reaccionar a objetivos logrados.");
            return objective;
        }

        // Nadie recibe una alerta por reaccionar a lo suyo.
        private async Task AvisarAlDuenioAsync(Objective objective, int requesterId, Func<string, CreateNotificationDto> armar)
        {
            if (objective.UserId == requesterId) return;
            var quien = await _userRepository.GetByIdAsync(requesterId);
            await _notificationService.CreateAsync(armar(quien?.Name ?? "Alguien"));
        }

        private static string Extracto(string texto)
            => texto.Length <= LargoDelExtracto ? texto : texto[..LargoDelExtracto].TrimEnd() + "…";

        private static ObjectiveCommentDto ToDto(ObjectiveComment c) => new()
        {
            Id = c.Id,
            ObjectiveId = c.ObjectiveId,
            UserId = c.UserId,
            Username = c.User?.Username ?? string.Empty,
            Name = c.User?.Name ?? string.Empty,
            ProfilePhotoUrl = c.User?.ProfilePhotoUrl,
            Content = c.Content,
            CreatedAt = c.CreatedAt
        };
    }
}
