﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Colyseus;
using DataChange = Colyseus.Schema.DataChange;

namespace BattleshipGame.Network
{
    public class NetworkClient : IClient
    {
        private const string RoomName = "game";
        private const string LobbyName = "lobby";
        private readonly Dictionary<string, Room> _rooms = new Dictionary<string, Room>();
        private Client _client;
        private Room<LobbyState> _lobby;
        private Room<State> _room;
        public event Action<string> GamePhaseChanged;

        public Connection GetRoomConnection()
        {
            return _room.Connection;
        }

        public Connection GetLobbyConnection()
        {
            return _lobby.Connection;
        }

        public State GetRoomState()
        {
            return _room?.State;
        }

        public string GetSessionId()
        {
            return _room?.SessionId;
        }

        public void SendPlacement(int[] placement)
        {
            _room.Send("place", placement);
        }

        public void SendTurn(int[] targetIndexes)
        {
            _room.Send("turn", targetIndexes);
        }

        public void SendRematch(bool isRematching)
        {
            _room.Send("rematch", isRematching);
        }

        public void LeaveRoom()
        {
            _room?.Leave();
            _room = null;
        }

        public async void Connect(string endPoint, Action success, Action error)
        {
            if (_lobby != null && _lobby.Connection.IsOpen) return;
            _client = new Client(endPoint);
            try
            {
                _lobby = await _client.JoinOrCreate<LobbyState>(LobbyName);
                success?.Invoke();
                RegisterLobbyHandlers();
            }
            catch (Exception)
            {
                error?.Invoke();
            }
        }

        public event Action<Dictionary<string, Room>> RoomsChanged;

        private void RegisterLobbyHandlers()
        {
            _lobby.OnMessage<Room[]>("rooms", message =>
            {
                foreach (var room in message)
                    if (!_rooms.ContainsKey(room.roomId))
                        _rooms.Add(room.roomId, room);

                RoomsChanged?.Invoke(_rooms);
            });

            _lobby.OnMessage<object[]>("+", message => { _lobby.Send("roomInfo", message[0]); });

            _lobby.OnMessage<Room>("roomInfo", room =>
            {
                if (room == null)
                    _rooms.Clear();
                else if (_rooms.ContainsKey(room.roomId))
                    _rooms[room.roomId] = room;
                else
                    _rooms.Add(room.roomId, room);

                RoomsChanged?.Invoke(_rooms);
            });

            _lobby.OnMessage<string>("-", roomId =>
            {
                if (!_rooms.ContainsKey(roomId)) return;
                _rooms.Remove(roomId);
                RoomsChanged?.Invoke(_rooms);
            });
        }

        public async void CreateRoom(string name, string password, Action<string> onError = null)
        {
            try
            {
                _room = await _client.Create<State>(RoomName,
                    new Dictionary<string, object> {{"name", name}, {"password", password}});
                RegisterRoomHandlers();
            }
            catch (Exception exception)
            {
                onError?.Invoke(exception.Message);
            }
        }

        public async void JoinRoom(string roomId, string password, Action<string> onError = null)
        {
            try
            {
                _room = await _client.JoinById<State>(roomId, new Dictionary<string, object> {{"password", password}});
                RegisterRoomHandlers();
            }
            catch (Exception exception)
            {
                onError?.Invoke(exception.Message);
            }
        }

        private void RegisterRoomHandlers()
        {
            _room.State.OnChange += OnRoomStateChange;

            void OnRoomStateChange(List<DataChange> changes)
            {
                foreach (var change in changes.Where(change => change.Field == "phase"))
                    GamePhaseChanged?.Invoke((string) change.Value);
            }
        }

        public void LeaveLobby()
        {
            _lobby?.Leave();
            _lobby = null;
        }

        public bool IsRoomPasswordProtected(string roomId)
        {
            return _rooms.TryGetValue(roomId, out var room) && room.metadata.requiresPassword;
        }

        public void RefreshRooms()
        {
            RoomsChanged?.Invoke(_rooms);
        }
    }
}