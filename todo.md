Below is a **`todo.md`** file you can include in your project. It’s a **checklist** aligned with the iterative plan and milestones we discussed. Simply copy and paste it into your repository. As you complete each item, you can place an `x` inside the `[ ]` to turn it into `[x]` for a checked box. 

---

# **todo.md**

## **Milestone A: Project Skeleton & Basic Test Setup**
- [x] **A.1**: Create core folder structure (e.g., `/src`, `/tests`, `/assets/json`).
- [x] **A.2**: Initialize project with a build system (`.csproj`, `package.json`, etc.).
- [x] **A.3**: Set up a testing framework (NUnit, xUnit, MSTest, pytest, etc.).
- [x] **A.4**: Create a trivial "Hello World" test and confirm it runs successfully.
- [x] **A.5**: Document build/test run instructions (e.g., `dotnet test`, `pytest`).

## **Milestone B: JSON Data Structures & Basic CRUD**
- [x] **B.1**: Create `Device` class/record with required properties (Name, Type, IP, MAC, Area, Category, SchedulerGroups, Commands, Dependencies, Schedule).
- [x] **B.2**: Create `Dependency` and `ScheduleEntry` classes/records.
- [x] **B.3**: Implement `JsonStorage.LoadDevices()` and `JsonStorage.SaveDevices()` to read/write `List<Device>`.
- [x] **B.4**: Write unit tests for loading/saving device data from `devices.json`.
- [x] **B.5**: Verify all tests pass and handle edge cases (empty or invalid JSON).

## **Milestone C: Device Management (Add/Edit/Remove)**
- [x] **C.1**: Implement a `DeviceManager` class to store devices in-memory.
- [x] **C.2**: Add methods: `AddDevice(Device device)`, `EditDevice(Device device)`, `RemoveDevice(string deviceName)`.
- [x] **C.3**: Ensure each method auto-saves changes to `devices.json`.
- [x] **C.4**: Write tests for add/edit/remove logic, verifying JSON updates.
- [x] **C.5**: Confirm removal of a device also removes references from groups, or handle gracefully.

## **Milestone D: Scheduling Logic & “Last Action Wins”**
- [x] **D.1**: Create a `Scheduler` class to parse device schedules (weekly or one-time).
- [x] **D.2**: Implement logic to determine actions (turn_on, turn_off) based on current time/day.
- [x] **D.3**: Handle conflicts by letting the last scheduled action in chronological order “win.”
- [x] **D.4**: Write unit tests for:
  - Single device with multiple schedules
  - Overlapping schedules testing “last action wins”
  - One-time events
- [x] **D.5**: Confirm tests pass, with schedules executed correctly.

## **Milestone E: Manual On/Off Commands & Test Stubs**
- [x] **E.1**: Add a `CommandExecutor` or similar class for manual device on/off.
- [x] **E.2**: For “on,” stub out (or implement) Wake-on-LAN; for “off,” stub out shutdown command.
- [x] **E.3**: Ensure manual toggles do not remove or alter future schedules.
- [x] **E.4**: Write tests simulating user-driven on/off, verifying logs or placeholders.
- [x] **E.5**: Verify that schedules remain intact after manual toggles.

## **Milestone F: Groups & Presets**
- [x] **F.1**: Let `Device.schedulerGroups` store membership for each group or preset.
- [x] **F.2**: Implement `TurnGroupOn(string groupName)` and `TurnGroupOff(string groupName)`.
- [x] **F.3**: On group removal, remove references from all devices.
- [x] **F.4**: Write tests:
  - Multiple devices in one group toggled on/off
  - Removing a group, ensuring devices no longer list it
- [x] **F.5**: Confirm everything persists in `devices.json` as expected.

## **Milestone G: Ping Monitoring & Online/Offline State**
- [x] **G.1**: Create `DeviceStatusMonitor` or similar to ping devices every 60s.
- [x] **G.2**: Increment a failure counter upon ping failure; after 3 consecutive failures, mark device offline.
- [x] **G.3**: Mark device online immediately when it responds.
- [x] **G.4**: Write tests for offline/online transitions (simulate ping failures, then successes).
- [x] **G.5**: Verify device status is updated in-memory and in the UI (later).

## **Milestone H: Dependency Handling & Power Sequencing**
- [ ] **H.1**: Add `dependencies` to each device; store `(dependsOn, delayMinutes)` pairs.
- [ ] **H.2**: On turn_on or turn_off, consider dependency delays. 
- [ ] **H.3**: If a dependency device is offline or removed, proceed anyway.
- [ ] **H.4**: Detect and block circular dependencies (basic check).
- [ ] **H.5**: Test multiple dependencies in a chain, verifying delayed ordering.

## **Milestone I: Power Strips & Outlet Control**
- [ ] **I.1**: Represent power strips as `Device` with a special `type = "power_strip"` and an array of outlets.
- [ ] **I.2**: Provide on/off commands for each outlet.
- [ ] **I.3**: If strip is offline, mark all outlets as unavailable.
- [ ] **I.4**: Test toggling outlets and simulate offline conditions.
- [ ] **I.5**: Confirm outlet state is tracked accurately.

## **Milestone J: Error Logging & Notifications**
- [ ] **J.1**: Create a logging mechanism that appends to `debug.log`.
- [ ] **J.2**: On command failure for an online device, retry once. If still failing, log the error.
- [ ] **J.3**: If device is offline, skip scheduled actions and log a “skipped” error/warning.
- [ ] **J.4**: Display logs in an Error Log or notifications (depending on UI).
- [ ] **J.5**: Test error conditions to confirm they are handled and logged properly.

## **Milestone K: UI/UX Integration**
- [ ] **K.1**: Create a **Main Dashboard** showing:
  - Today’s scheduled actions
  - Global scheduler toggle
  - Main on/off button for “Main Mediatechnik Preset”
  - Offline devices list
- [ ] **K.2**: Implement area/cluster tabs or category tabs to view relevant devices and toggle individually.
- [ ] **K.3**: Add a scheduler management interface to add/edit schedules (weekly + one-time).
- [ ] **K.4**: Integrate error log view for real-time or near-real-time display.
- [ ] **K.5**: Ensure all integrations are tested end-to-end, verifying correct backend calls.

## **Milestone L: Auto-Update, Localization & Final Checks**
- [ ] **L.1**: Implement auto-update check against a GitHub repo. Prompt user if a new version exists.
- [ ] **L.2**: Add basic localization (English/German) with user-selectable settings.
- [ ] **L.3**: Do a final pass of user acceptance tests (UAT) with typical scenarios (device scheduling, manual override, etc.).
- [ ] **L.4**: Review error handling, logs, and performance (scale test if needed).
- [ ] **L.5**: Package or publish the final application and document release instructions.

---

**End of `todo.md`**