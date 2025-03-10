using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExControl.Models;
using ExControl.Services;

namespace ExControl.Tests
{
    [TestClass]
    public class PowerStripTests
    {
        private ManualControlService _controlService;

        [TestInitialize]
        public void Setup()
        {
            _controlService = new ManualControlService();
        }

        [TestMethod]
        public void TurnOnOutlet_Succeeds_WhenStripOnline()
        {
            // Arrange
            var powerStrip = new Device
            {
                Name = "Strip-1",
                Type = "power_strip",
                IsOnline = true, // the strip is online
                Outlets = new List<Outlet>
                {
                    new Outlet
                    {
                        Name = "Outlet #1",
                        Commands = new Dictionary<string,string>
                        {
                            {"on", "outlet1_on_cmd"},
                            {"off", "outlet1_off_cmd"}
                        }
                    },
                    new Outlet
                    {
                        Name = "Outlet #2",
                        Commands = new Dictionary<string,string>
                        {
                            {"on", "outlet2_on_cmd"},
                            {"off", "outlet2_off_cmd"}
                        }
                    }
                }
            };

            // Act
            bool success1 = _controlService.TurnOutletOn(powerStrip, 0);
            bool success2 = _controlService.TurnOutletOn(powerStrip, 1);

            // Assert
            Assert.IsTrue(success1, "Should succeed toggling first outlet on an online strip.");
            Assert.IsTrue(success2, "Should succeed toggling second outlet on an online strip.");

            Assert.IsTrue(powerStrip.Outlets[0].IsOn, "Outlet #1 must be marked on.");
            Assert.IsTrue(powerStrip.Outlets[1].IsOn, "Outlet #2 must be marked on.");
        }

        [TestMethod]
        public void TurnOnOutlet_Fails_WhenStripOffline()
        {
            // Arrange
            var powerStrip = new Device
            {
                Name = "Strip-2",
                Type = "power_strip",
                IsOnline = false, // strip is offline
                Outlets = new List<Outlet>
                {
                    new Outlet { Name="Outlet #1" }
                }
            };

            // Act
            bool result = _controlService.TurnOutletOn(powerStrip, 0);

            // Assert
            Assert.IsFalse(result, "Should fail because the strip is offline.");
            Assert.IsFalse(powerStrip.Outlets[0].IsOn, "Outlet #1 must remain off if the strip is offline.");
        }

        [TestMethod]
        public void TurnOffOutlet_Succeeds_WhenStripOnline()
        {
            // Arrange
            var powerStrip = new Device
            {
                Name = "PS-Online",
                Type = "power_strip",
                IsOnline = true,
                Outlets = new List<Outlet>
                {
                    new Outlet
                    {
                        Name = "Outlet #A",
                        IsOn = true, // Already turned on
                        Commands = new Dictionary<string,string>
                        {
                            {"on", "outletA_on"},
                            {"off", "outletA_off"}
                        }
                    }
                }
            };

            // Act
            bool turnOff = _controlService.TurnOutletOff(powerStrip, 0);

            // Assert
            Assert.IsTrue(turnOff, "Should be able to turn off an outlet on an online strip.");
            Assert.IsFalse(powerStrip.Outlets[0].IsOn, "Outlet #A should now be off.");
        }

        [TestMethod]
        public void TurnOffOutlet_Fails_WhenStripOffline()
        {
            // Arrange
            var powerStrip = new Device
            {
                Name = "PS-Offline",
                Type = "power_strip",
                IsOnline = false,
                Outlets = new List<Outlet>
                {
                    new Outlet { Name = "OutletX", IsOn = true }
                }
            };

            // Act
            bool success = _controlService.TurnOutletOff(powerStrip, 0);

            // Assert
            Assert.IsFalse(success, "Should fail if the strip is offline.");
            Assert.IsTrue(powerStrip.Outlets[0].IsOn, "Outlet should remain on because we couldn't turn it off offline.");
        }

        [TestMethod]
        public void InvalidIndex_Fails()
        {
            // Arrange
            var powerStrip = new Device
            {
                Name = "PS-FewOutlets",
                Type = "power_strip",
                IsOnline = true,
                Outlets = new List<Outlet>
                {
                    new Outlet { Name="O1" }
                }
            };

            // Act
            bool resultNegative = _controlService.TurnOutletOn(powerStrip, -1);
            bool resultOutOfRange = _controlService.TurnOutletOn(powerStrip, 100);

            // Assert
            Assert.IsFalse(resultNegative, "Cannot turn on an invalid negative index.");
            Assert.IsFalse(resultOutOfRange, "Cannot turn on an index out of range.");
        }

        [TestMethod]
        public void NotAPowerStrip_Fails()
        {
            // Arrange
            var normalDevice = new Device
            {
                Name = "RegularPC",
                Type = "pc",
                IsOnline = true
            };

            // Act
            // Attempt to turn outlet #0 on
            var success = _controlService.TurnOutletOn(normalDevice, 0);

            // Assert
            Assert.IsFalse(success, "Turning an 'outlet' on a normal PC is invalid.");
        }
    }
}
