using Blish_HUD;
using Blish_HUD.ArcDps.Models;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
        public bool Enraged { get => _enrageTimer < DateTime.Now.Subtract(_startTime).Milliseconds; }

        public IReadOnlyList<double> Phases { get; private set; }
        public IReadOnlyList<long> Times { get; private set; }

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

        private float _playerSpawnSize;
        private long _enrageTimer;
        private DateTime _startTime;
        private readonly long _initialHealth;

        public Encounter(EncounterData data) {
            Name = data.Name;
            Ids = data.Ids;
            _initialHealth = data.Health;
            Health = data.Health;
            Times = data.Times;
            Phases = data.Phases;
            _playerSpawnSize = 50.0f;
            PlayerSpawns = data.PlayerSpawns;
            CurrentPhase = 0;
            _enrageTimer = data.EnrageTimer;
            _startTime = DateTime.Now;
        }
        /// <summary>
        /// Tries to decrement the current health given the damaging combat event.
        /// </summary>
        /// <param name="e">A combat event with damaging fields.</param>
        public void DoDamage(Ev e) {
            var damage = 0;

            // buff damage event
            if (e.Buff && e.Value == 0)
                damage += e.BuffDmg;
            // direct damage event
            else if (!e.Buff && e.Value > 0)
                damage += e.Value;

            if (damage == 0) return;

            switch (e.Result) {
                case 3: return; // blocked
                case 4: return; // evaded
                case 6: return; // absorbed
                case 7: return; // missed
                default: Health -= Math.Abs(damage); break;
            }

            if (CurrentPhase < Phases.Count - 1 && Health < Phases[CurrentPhase] * _initialHealth)
                CurrentPhase++;
        }
        /// <summary>
        /// Gets the current health percentage.
        /// </summary>
        /// <returns></returns>
        public float GetHealthPercent() {
            return _initialHealth == 0 ? 0 : Health / (float)_initialHealth;
        }

        public bool IsPlayerReset(Vector3 position) {
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
