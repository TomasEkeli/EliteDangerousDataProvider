﻿using Eddi;
using EddiCompanionAppService;
using EddiDataDefinitions;
using EddiEvents;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Utilities;

namespace EddiShipMonitor
{
    public class ShipMonitor : EDDIMonitor
    {
        // Observable collection for us to handle
        public ObservableCollection<Ship> shipyard = new ObservableCollection<Ship>();
        public Ship ship { get; set; } = new Ship();

        public string MonitorName()
        {
            return "Ship monitor";
        }

        public string MonitorVersion()
        {
            return "1.0.0";
        }

        public string MonitorDescription()
        {
            return "Track information on your ships.";
        }

        public bool IsRequired()
        {
            return true;
        }

        public ShipMonitor()
        {
            readShips();
            Logging.Info("Initialised " + MonitorName() + " " + MonitorVersion());
        }

        public void Start()
        {
            // We don't actively do anything, just listen to events, so nothing to do here
        }

        public void Stop()
        {
        }

        public void Reload()
        {
            readShips();
            Logging.Info("Reloaded " + MonitorName() + " " + MonitorVersion());
        }

        public UserControl ConfigurationTabItem()
        {
            return null;
//            return new ConfigurationWindow();
        }

        public void Handle(Event @event)
        {
            Logging.Debug("Received event " + JsonConvert.SerializeObject(@event));

            // Handle the events that we care about
            if (@event is ShipSwappedEvent)
            {
                handleShipSwappedEvent((ShipSwappedEvent)@event);
            }
            else if (@event is ShipSoldEvent)
            {
                handleShipSoldEvent((ShipSoldEvent)@event);
            }
            else if (@event is ShipPurchasedEvent)
            {
                handleShipPurchasedEvent((ShipPurchasedEvent)@event);
            }
            else if (@event is ShipDeliveredEvent)
            {
                handleShipDeliveredEvent((ShipDeliveredEvent)@event);
            }
            else if (@event is ShipRenamedEvent)
            {
                handleShipRenamedEvent((ShipRenamedEvent)@event);
            }
            // TODO loadout
        }

        private void handleShipSwappedEvent(ShipSwappedEvent @event)
        {
            SetCurrentShip(@event.Ship);
        }

        private void handleShipSoldEvent(ShipSoldEvent @event)
        {
            for (int i = 0; i < shipyard.Count; i++)
            {
                if (shipyard[i].LocalId == @event.shipid)
                {
                    shipyard.RemoveAt(i);
                    break;
                }
            }
        }

        private void handleShipPurchasedEvent(ShipPurchasedEvent @event)
        {
        }

        private void handleShipDeliveredEvent(ShipDeliveredEvent @event)
        {
            SetCurrentShip(@event.Ship);
        }

        private void handleShipRenamedEvent(ShipRenamedEvent @event)
        {
            if (ship.LocalId == @event.Ship.LocalId)
            {
                // This ship
                @event.Ship.name = @event.name;
                if (@event.ident != null)
                {
                    @event.Ship.ident = @event.ident;
                }
            }
            writeShips();
        }

        private void handleCommanderContinuedEvent(CommanderContinuedEvent @event)
        {
            SetCurrentShip(@event.Ship);
        }

        public void Handle(Profile profile)
        {
            // Information from the Frontier API can be out-of-date so we only use it to augment what we already have

            // Reset the shipyard from the profile information
            //Shipyard.Clear();
            //foreach (Ship ship in profile.Shipyard)
            //{
            //    Shipyard.Add(ship);
            //}

            //// Only use the ship information if we agree that this is the correct ship to use
            //if (profile.Ship != null && (Ship.model == null || profile.Ship.LocalId == Ship.LocalId))
            //{
            //    SetShip(profile.Ship);
            //}

        }

        public IDictionary<string, object> GetVariables()
        {
            IDictionary<string, object> variables = new Dictionary<string, object>();
            variables["ship"] = ship;
            variables["shipyard"] = shipyard;

            return variables;
        }

        public void writeShips()
        {
            // Write ship configuration with current inventory
            ShipMonitorConfiguration configuration = new ShipMonitorConfiguration();
            configuration.ship = ship;
            configuration.shipyard = shipyard;
            configuration.ToFile();
        }

        private void readShips()
        {
            // Obtain current inventory from  configuration
            ShipMonitorConfiguration configuration = ShipMonitorConfiguration.FromFile();

            // Build a new shipyard
            List<Ship> newShipyard = new List<Ship>();
            foreach (Ship ship in configuration.shipyard)
            {
                newShipyard.Add(ship);
            }

            // Now order the list by model
            newShipyard = newShipyard.OrderBy(s => s.model).ToList();

            // Update the shipyard
            shipyard.Clear();
            foreach (Ship ship in newShipyard)
            {
                shipyard.Add(ship);
            }
        }

        private void SetCurrentShip(Ship ship)
        {
            if (ship == null)
            {
                Logging.Warn("Refusing to set ship to null");
                return;
            }

            if (ship != null)
            {
                // Remove the ship we are now using from the shipyard
                int shipIndex = -1;
                for (int i = 0; i < shipyard.Count; i++)
                {
                    if (shipyard[i].LocalId == ship.LocalId)
                    {
                        shipIndex = i;
                        break;
                    }
                }
                if (shipIndex > -1)
                {
                    shipyard.RemoveAt(shipIndex);
                }

                // Add the ship we were using to the shipyard (if it's real)
                if (ship.model != null)
                {
                    shipyard.Add(ship);
                }
            }

            // Set the ship we are using
            Logging.Debug("Set ship to " + JsonConvert.SerializeObject(ship));
            this.ship = ship;
        }
    }
}