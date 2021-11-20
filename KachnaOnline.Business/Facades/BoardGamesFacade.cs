// BoardGamesFacade.cs
// Author: František Nečas

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using KachnaOnline.Business.Models.BoardGames;
using KachnaOnline.Business.Services.Abstractions;
using KachnaOnline.Dto.BoardGames;
using KachnaOnline.Business.Constants;
using KachnaOnline.Business.Exceptions;
using KachnaOnline.Business.Exceptions.BoardGames;
using ReservationState = KachnaOnline.Dto.BoardGames.ReservationState;

namespace KachnaOnline.Business.Facades
{
    public class BoardGamesFacade
    {
        private readonly IMapper _mapper;
        private readonly IBoardGamesService _boardGamesService;

        public BoardGamesFacade(IBoardGamesService boardGamesService, IMapper mapper)
        {
            _boardGamesService = boardGamesService;
            _mapper = mapper;
        }

        /// <summary>
        /// Returns all categories of board games.
        /// </summary>
        /// <returns>A list of <see cref="CategoryDto"/>.</returns>
        public async Task<IEnumerable<CategoryDto>> GetCategories()
        {
            var categories = await _boardGamesService.GetBoardGameCategories();
            return _mapper.Map<List<CategoryDto>>(categories);
        }

        /// <summary>
        /// Returns a category with the given ID.
        /// </summary>
        /// <param name="categoryId">ID of the <see cref="CategoryDto"/> to return.</param>
        /// <returns>A <see cref="CategoryDto"/> with the given ID.</returns>
        /// <exception cref="CategoryNotFoundException">Thrown when a category with the given
        /// <paramref name="categoryId"/> does not exist.</exception>
        public async Task<CategoryDto> GetCategory(int categoryId)
        {
            return _mapper.Map<CategoryDto>(await _boardGamesService.GetCategory(categoryId));
        }

        /// <summary>
        /// Creates a new category.
        /// </summary>
        /// <param name="category"><see cref="CreateCategoryDto"/> to create.</param>
        /// <returns>The created <see cref="CategoryDto"/> with a filled ID.</returns>
        /// <exception cref="CategoryManipulationFailedException">Thrown when the category cannot be created.
        /// This can be caused by a database error.</exception>
        public async Task<CategoryDto> CreateCategory(CreateCategoryDto category)
        {
            var createdCategory = await _boardGamesService.CreateCategory(_mapper.Map<Category>(category));
            return _mapper.Map<CategoryDto>(createdCategory);
        }

        /// <summary>
        /// Updates a category with the given ID.
        /// </summary>
        /// <param name="id">ID of the category to update.</param>
        /// <param name="category"><see cref="CreateCategoryDto"/> representing the new state.</param>
        /// <exception cref="CategoryNotFoundException">When a category with the given <paramref name="id"/> does not
        /// exist.</exception>
        /// <exception cref="CategoryManipulationFailedException">When the category cannot be updated.</exception>
        public async Task UpdateCategory(int id, CreateCategoryDto category)
        {
            await _boardGamesService.UpdateCategory(id, _mapper.Map<Category>(category));
        }

        /// <summary>
        /// Deletes a category with the given ID.
        /// </summary>
        /// <param name="id">ID of the category to delete.</param>
        /// <exception cref="CategoryNotFoundException">When a category with the given <paramref name="id"/> does not
        /// exist.</exception>
        /// <exception cref="CategoryManipulationFailedException">When the category cannot be deleted.</exception>
        /// <exception cref="CategoryHasBoardGamesException">When the category has linked board games.</exception>
        public async Task DeleteCategory(int id)
        {
            await _boardGamesService.DeleteCategory(id);
        }

        /// <summary>
        /// Returns the list of board games.
        /// </summary>
        /// <param name="user">User requesting the data.</param>
        /// <param name="categoryId">If set, returns only the board games of this category ID.</param>
        /// <param name="players">If set, returns only board games which can be played by this many players.</param>
        /// <param name="available">If set, returns only the board games of this availability.</param>
        /// <param name="visible">If set, returns only the boards games with this visibility. It is implicitly
        /// set to true for unauthenticated or regular users.</param>
        /// <returns>A list of <see cref="BoardGameDto"/>. If the <paramref name="user"/> is an authorized
        /// board games manager, returns a list of <see cref="ManagerBoardGameDto"/> instead.</returns>
        public async Task<IEnumerable<BoardGameDto>> GetBoardGames(
            ClaimsPrincipal user,
            int? categoryId,
            int? players,
            bool? available,
            bool? visible)
        {
            // Regular users can only see visible games
            if (!user.IsInRole(RoleConstants.BoardGamesManager))
                visible = true;

            var games = await _boardGamesService.GetBoardGames(categoryId, players, available, visible);
            if (user.IsInRole(RoleConstants.BoardGamesManager))
            {
                return _mapper.Map<List<ManagerBoardGameDto>>(games);
            }

            return _mapper.Map<List<BoardGameDto>>(games);
        }

        /// <summary>
        /// Returns a board game with the given ID.
        /// </summary>
        /// <param name="user">User requesting the data.</param>
        /// <param name="boardGameId">ID of the <see cref="BoardGameDto"/> to return.</param>
        /// <returns>A <see cref="BoardGameDto"/> with the given ID. If the <paramref name="user"/> is an authorized
        /// board games manager, returns <see cref="ManagerBoardGameDto"/> instead.</returns>
        /// <exception cref="BoardGameNotFoundException">Thrown when a board game with the given
        /// <paramref name="boardGameId"/> does not exist.</exception>
        /// <exception cref="NotAuthenticatedException">Thrown when a board game with the given
        /// <paramref name="boardGameId"/> exists but is invisible and the <paramref name="user"/>
        /// is not authenticated.</exception>
        /// <exception cref="NotABoardGamesManagerException">Thrown when a board game with the given
        /// <paramref name="boardGameId"/> exists but is invisible and the <paramref name="user"/>
        /// is not a board games manager.</exception>
        public async Task<BoardGameDto> GetBoardGame(ClaimsPrincipal user, int boardGameId)
        {
            var game = await _boardGamesService.GetBoardGame(boardGameId);
            if (!game.Visible)
            {
                if (!(user.Identity?.IsAuthenticated ?? false))
                {
                    throw new NotAuthenticatedException();
                }

                if (!user.IsInRole(RoleConstants.BoardGamesManager))
                {
                    throw new NotABoardGamesManagerException();
                }
            }

            if (user.IsInRole(RoleConstants.BoardGamesManager))
            {
                return _mapper.Map<ManagerBoardGameDto>(game);
            }

            return _mapper.Map<BoardGameDto>(game);
        }

        /// <summary>
        /// Creates a new board game.
        /// </summary>
        /// <param name="game"><see cref="CreateBoardGameDto"/> to create.</param>
        /// <returns>The created <see cref="ManagerBoardGameDto"/> with a filled ID.</returns>
        /// <exception cref="BoardGameManipulationFailedException">Thrown when the board game cannot be created.
        /// This can be caused by a database error.</exception>
        /// <exception cref="CategoryNotFoundException">When a category with the ID assigned to the game
        /// does not exist.</exception>
        /// <exception cref="UserNotFoundException">When a user with the ID assigned to the game does
        /// not exist.</exception>
        public async Task<BoardGameDto> CreateBoardGame(CreateBoardGameDto game)
        {
            var createdGame = await _boardGamesService.CreateBoardGame(_mapper.Map<BoardGame>(game));
            return _mapper.Map<ManagerBoardGameDto>(createdGame);
        }

        /// <summary>
        /// Updates a board game with the given ID.
        /// </summary>
        /// <param name="id">ID of the board game to update.</param>
        /// <param name="game"><see cref="CreateBoardGameDto"/> representing the new state.</param>
        /// <exception cref="BoardGameNotFoundException">When a board game with the given <paramref name="id"/> does not
        /// exist.</exception>
        /// <exception cref="BoardGameManipulationFailedException">When the board game cannot be updated.</exception>
        /// <exception cref="CategoryNotFoundException">When a category with the ID assigned to the game
        /// does not exist.</exception>
        /// <exception cref="UserNotFoundException">When a user with the ID assigned to the game does
        /// not exist.</exception>
        public async Task UpdateBoardGame(int id, CreateBoardGameDto game)
        {
            await _boardGamesService.UpdateBoardGame(id, _mapper.Map<BoardGame>(game));
        }

        /// <summary>
        /// Updates stock of a board game with the given ID.
        /// </summary>
        /// <param name="id">ID of the board game to update.</param>
        /// <param name="stock"><see cref="BoardGameStockDto"/> representing the new stock state.</param>
        /// <exception cref="BoardGameNotFoundException">When a board game with the given <paramref name="id"/> does not
        /// exist.</exception>
        /// <exception cref="BoardGameManipulationFailedException">When the board game cannot be updated.</exception>
        public async Task UpdateBoardGameStock(int id, BoardGameStockDto stock)
        {
            await _boardGamesService.UpdateBoardGame(id, stock.InStock, stock.Unavailable, stock.Visible);
        }

        /// <summary>
        /// Returns a list of all user's reservation.
        /// </summary>
        /// <param name="user">User to get reservations of.</param>
        /// <param name="state">Optional reservation filter based on state.</param>
        /// <returns>The list of user's reservation.</returns>
        public async Task<IEnumerable<ReservationDto>> GetUserReservations(int user,
            ReservationState? state)
        {
            var stateModel = _mapper.Map<Models.BoardGames.ReservationState?>(state);
            var dtos = new List<ReservationDto>();
            foreach (var reservation in await _boardGamesService.GetUserReservations(user, stateModel))
            {
                var dto = _mapper.Map<ReservationDto>(reservation);
                dto.Items = _mapper.Map<ReservationItemDto[]>(await _boardGamesService.GetReservationItems(dto.Id));
                dtos.Add(dto);
            }

            return dtos;
        }

        /// <summary>
        /// Returns a list of all reservations.
        /// </summary>
        /// <param name="state">Optional reservation filter based on state.</param>
        /// <param name="assignedTo">Optional reservation filter based on assigned board games manager ID.</param>
        /// <returns>The list of all board games reservations, filtered by the given filters if requested.</returns>
        public async Task<IEnumerable<ManagerReservationDto>> GetAllReservations(ReservationState? state,
            int? assignedTo)
        {
            var stateModel = _mapper.Map<Models.BoardGames.ReservationState?>(state);
            var dtos = new List<ManagerReservationDto>();
            foreach (var reservation in await _boardGamesService.GetAllReservations(stateModel, assignedTo))
            {
                var dto = _mapper.Map<ManagerReservationDto>(reservation);
                dto.Items = _mapper.Map<ReservationItemDto[]>(await _boardGamesService.GetReservationItems(dto.Id));
                dtos.Add(dto);
            }

            return dtos;
        }

        /// <summary>
        /// Returns a single reservation.
        /// </summary>
        /// <param name="user">Currently authenticated user.</param>
        /// <param name="reservationId">ID of the reservation to return.</param>
        /// <returns>Reservation with ID <paramref name="reservationId"/>. Returns a
        /// <see cref="ManagerReservationDto"/> if the requesting <paramref name="user"/>"/> is a board games
        /// manager.</returns>
        /// <exception cref="NotABoardGamesManagerException">Thrown when <paramref name="user"/> is not a board games
        /// manager and the reservation is owned by someone else.</exception>
        /// <exception cref="ReservationNotFoundException">When a reservation with <paramref name="reservationId"/>
        /// does not exist.</exception>
        public async Task<ReservationDto> GetReservation(ClaimsPrincipal user, int reservationId)
        {
            var reservation = await _boardGamesService.GetReservation(reservationId);
            if (user.IsInRole(RoleConstants.BoardGamesManager))
            {
                var managerDto = _mapper.Map<ManagerReservationDto>(reservation);
                managerDto.Items =
                    _mapper.Map<ReservationItemDto[]>(await _boardGamesService.GetReservationItems(managerDto.Id));
                return managerDto;
            }

            if (reservation.MadeById != int.Parse(user.FindFirstValue(IdentityConstants.IdClaim)))
            {
                throw new NotABoardGamesManagerException();
            }

            var userDto = _mapper.Map<ReservationDto>(reservation);
            userDto.Items = _mapper.Map<ReservationItemDto[]>(await _boardGamesService.GetReservationItems(userDto.Id));
            return userDto;
        }

        /// <summary>
        /// Updates user note in a reservation.
        /// </summary>
        /// <param name="id">ID of the reservation to update.</param>
        /// <param name="userId">ID of the user requesting the change.</param>
        /// <param name="note"><see cref="ReservationNoteUserDto"/> containing the new user note.</param>
        /// <exception cref="ReservationNotFoundException">When no such reservation exists.</exception>
        /// <exception cref="ReservationAccessDeniedException">When the user does not own the reservation.</exception>
        /// <exception cref="ReservationManipulationFailedException">When the reservation cannot be modified.</exception>
        public async Task UpdateReservationNote(int id, int userId, ReservationNoteUserDto note)
        {
            await _boardGamesService.UpdateReservationNote(id, userId, note.NoteUser);
        }

        /// <summary>
        /// Updates internal note in a reservation.
        /// </summary>
        /// <param name="id">ID of the reservation to update.</param>
        /// <param name="note"><see cref="ReservationNoteInternalDto"/> containing the new internal note.</param>
        /// <exception cref="ReservationNotFoundException">When no such reservation exists.</exception>
        /// <exception cref="ReservationManipulationFailedException">When the reservation cannot be modified.</exception>
        public async Task UpdateReservationNoteInternal(int id, ReservationNoteInternalDto note)
        {
            await _boardGamesService.UpdateReservationNoteInternal(id, note.NoteInternal);
        }

        /// <summary>
        /// Returns state history of a single item.
        /// </summary>
        /// <param name="reservationId">ID of reservation the item belongs to.</param>
        /// <param name="itemId">ID of an item to get history of.</param>
        /// <returns>List of <see cref="ReservationItemEventDto"/> sorted from oldest to newest.</returns>
        /// <exception cref="ReservationNotFoundException">When no such item exists.</exception>
        public async Task<IEnumerable<ReservationItemEventDto>> GetItemHistory(int reservationId, int itemId)
        {
            var events = await _boardGamesService.GetItemHistory(reservationId, itemId);
            return _mapper.Map<IEnumerable<ReservationItemEventDto>>(events);
        }

        /// <summary>
        /// Creates a new reservation for a user.
        /// </summary>
        /// <param name="userId">ID of the user requesting the reservation.</param>
        /// <param name="reservation"><see cref="CreateReservationDto"/> containing the requested games.</param>
        /// <returns>The created <see cref="ReservationDto"/>.</returns>
        /// <exception cref="GameUnavailableException">When the whole request cannot be satisfied due to a game
        /// not being available.</exception>
        /// <exception cref="BoardGameNotFoundException">When a requested game does not exist.</exception>
        /// <exception cref="ReservationManipulationFailedException">When the reservation created failed.</exception>
        /// <exception cref="UserNotFoundException">When a user with ID <paramref name="userId"/> does not exist.
        /// Should not normally happen.</exception>
        public async Task<ReservationDto> CreateNewReservation(int userId, CreateReservationDto reservation)
        {
            var reservationModel = _mapper.Map<Reservation>(reservation);
            reservationModel.MadeById = userId;
            var created =
                await _boardGamesService.CreateReservation(reservationModel, userId, reservation.BoardGameIds);
            var createdDto = _mapper.Map<ReservationDto>(created);
            createdDto.Items =
                _mapper.Map<ReservationItemDto[]>(await _boardGamesService.GetReservationItems(createdDto.Id));
            return createdDto;
        }

        /// <summary>
        /// Creates a new reservation for a user by someone else (a board games manager).
        /// </summary>
        /// <param name="madeBy">ID of the user who is creating the reservation.</param>
        /// <param name="madeFor">ID of the user who the games are reserved for.</param>
        /// <param name="reservation"><see cref="ManagerCreateReservationDto"/> containing the requested games.</param>
        /// <returns>The created <see cref="ManagerReservationDto"/>.</returns>
        /// <exception cref="GameUnavailableException">When the whole request cannot be satisfied due to a game
        /// not being available.</exception>
        /// <exception cref="BoardGameNotFoundException">When a request game does not exist.</exception>
        /// <exception cref="ReservationManipulationFailedException">When the reservation created failed.</exception>
        /// <exception cref="UserNotFoundException">When a user with ID <paramref name="madeFor"/> does not exist.</exception>
        public async Task<ManagerReservationDto> ManagerCreateNewReservation(int madeBy, int madeFor,
            ManagerCreateReservationDto reservation)
        {
            var reservationModel = _mapper.Map<Reservation>(reservation);
            reservationModel.MadeById = madeFor;
            var created =
                await _boardGamesService.CreateReservation(reservationModel, madeBy, reservation.BoardGameIds);
            var createdDto = _mapper.Map<ManagerReservationDto>(created);
            createdDto.Items =
                _mapper.Map<ReservationItemDto[]>(await _boardGamesService.GetReservationItems(createdDto.Id));
            return createdDto;
        }

        /// <summary>
        /// Adds new items to a reservation.
        /// </summary>
        /// <param name="reservationId">ID of the reservation to add items to.</param>
        /// <param name="addedBy">ID of the user who is adding the games.</param>
        /// <param name="items"><see cref="UpdateReservationItemsDto"/> containing the new items.</param>
        /// <exception cref="BoardGameNotFoundException">When a requested game does not exist.</exception>
        /// <exception cref="GameUnavailableException">When some of the requested board games are not
        /// available.</exception>
        /// <exception cref="ReservationManipulationFailedException">When the reservation cannot be created.</exception>
        /// <exception cref="ReservationNotFoundException">When a reservation with ID
        /// <paramref name="reservationId"/> does not exist.</exception>
        /// <exception cref="UserNotFoundException">When a user with ID <paramref name="addedBy"/> does not exist.</exception>
        public async Task AddReservationItems(int reservationId, int addedBy, UpdateReservationItemsDto items)
        {
            await _boardGamesService.AddReservationItems(reservationId, addedBy, items.BoardGameIds);
        }
    }
}