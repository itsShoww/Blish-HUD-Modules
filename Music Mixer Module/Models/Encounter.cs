using Blish_HUD;
using Blish_HUD.ArcDps;
using Blish_HUD.ArcDps.Models;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using static Blish_HUD.ArcDps.ArcDpsEnums;
using static Blish_HUD.GameService;
namespace Nekres.Music_Mixer
{
    internal class Encounter
    {
        /// <summary>
        /// The name.
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Relevant agent ids.
        /// </summary>
        public IReadOnlyList<uint> Ids { get; private set; }

        /// <summary>
        /// The current health.
        /// </summary>
        public long Health { get; private set; }

        /// <summary>
        /// The playerspawn when the squad resets.
        /// </summary>
        public IReadOnlyList<Vector3> PlayerSpawns { get; private set; }

        /// <summary>
        /// If the encounter is enraged.
        /// </summary>
        /// <remarks>
        /// Ie. enrage timer has passed.
        /// </remarks>
        public bool Enraged { get => _enrageTimeMs < DateTime.Now.Subtract(_startTime).Milliseconds; }

        public IReadOnlyList<double> Phases { get; private set; }
        public IReadOnlyList<long> Times { get; private set; }

        public bool IsDead { get; private set; }

        public event EventHandler<ValueEventArgs<int>> PhaseChanged;

        private int _currentPhase;
        public int CurrentPhase { 
            get => _currentPhase;
            private set {
                if (_currentPhase == value || value > Phases.Count) return;

                _currentPhase = value;

                PhaseChanged?.Invoke(this, new ValueEventArgs<int>(value));
            }
        }

        private const float _playerSpawnSize = 50f;
        private long _enrageTimeMs;
        private DateTime _startTime;

        public Encounter(EncounterData data) {
            _enrageTimeMs = data.EnrageTimer;

            Name = data.Name;
            Ids = data.Ids;
            Health = data.Health;
            Times = data.Times;
            Phases = data.Phases;
            PlayerSpawns = data.PlayerSpawns;

            CurrentPhase = 0;

            _startTime = DateTime.Now;
        }


        public void CheckPhase(RawCombatEventArgs e) {
            if (e.CombatEvent.Ev.Iff == IFF.Foe && e.CombatEvent.Src.Self == 0)
                IsDead = e.CombatEvent.Ev.IsStateChange == StateChange.ChangeDead;
            if (e.CombatEvent.Ev.IsFifty)
                CurrentPhase++;
        }


        public bool IsPlayerReset() {
            var position = Gw2Mumble.PlayerCharacter.Position;
            foreach (var spawn in PlayerSpawns) {
                var top = new Vector3(spawn.X + _playerSpawnSize, spawn.Y + _playerSpawnSize, spawn.Z + _playerSpawnSize);
                var bot = new Vector3(spawn.X - _playerSpawnSize, spawn.Y - _playerSpawnSize, spawn.Z - _playerSpawnSize);
                if (((position.X > top.X && position.X < bot.X) ||
                    (position.X < top.X && position.X > bot.X)) &&
                    ((position.Y > top.Y && position.Y < bot.Y) ||
                    (position.Y < top.Y && position.Y > bot.Y)) &&
                    ((position.Z > top.Z && position.Z < bot.Z) ||
                    (position.Z < top.Z && position.Z > bot.Z)))
                    return true;
            }
            return false;
        }
    }
}
