Below is a **final, consolidated specification** (“spec.md”) that reflects **all** of the **final decisions** we arrived at during our step-by-step conversation. This is intended as a **developer-ready document** that outlines requirements, architecture, data handling, error handling strategies, and a testing plan. It reconciles any discrepancies and ensures the developer has a clear roadmap for implementation.

---

# **spec.md**

## **1. Overview**

This application manages and controls various museum devices (PCs, projectors, power strips, etc.) from a **central Windows PC**. Users can schedule on/off actions, turn devices on/off manually, and group devices for bulk operations. The UI will be a **hybrid approach** (Windows-based backend with a modern, visual interface, potentially providing a web dashboard as well).

---

## **2. Key Functional Requirements**

1. **Device Management**  
   - Store device details in a **JSON** file (e.g., `C:\MuseumControl\devices.json`).  
   - Users can **add/edit/remove** devices via a GUI (no input validation).  
   - Device info includes:  
     - Name (not necessarily unique)  
     - Type (PC, projector, power strip, etc.)  
     - IP address (manually maintained)  
     - MAC address (for Wake-on-LAN if applicable)  
     - Commands (e.g., shutdown, power on, projector on/off codes)  
     - Group assignments (see below)  

2. **Group Assignments and Categories**  
   - Each device can belong to **exactly one device category** (e.g., `PCs`, `Projectors`) and **exactly one area/cluster group** (e.g., `Democracy`, `Mobility`).  
   - A device can be a member of **multiple scheduler groups (presets)** for automation.  
   - The **“Main Mediatechnik Preset”** is a special group for the **global On/Off** button on the main dashboard.  
   - When a group/preset is **deleted**, devices are automatically **unassigned** from that group (but remain in other groups).

3. **Scheduling & Automation**  
   - **Schedules are stored within each device’s JSON entry**, under a `schedule` array.  
   - Users can define **custom weekly schedules** and **one-time events**.  
   - A **global scheduler toggle** can enable/disable all scheduled actions.  
   - If a device is assigned to multiple scheduler groups, it will combine those schedules. In case of conflicting actions, **the last action executed “wins.”**  
   - Missed actions (due to the app or device being offline) are **skipped**; the app does not retry them later.

4. **Manual Control vs. Scheduling**  
   - **Manual overrides** (turning a device on/off manually) do **not** cancel or alter future schedules.  
   - Scheduled actions always **execute as planned**, even if the device was manually toggled just before.

5. **Power Strips**  
   - Each power strip is displayed as a **single device** in the UI.  
   - When expanded, it shows **individual outlets** for on/off control.  
   - The app does **not** track which specific device is physically connected to which outlet.  
   - If a power strip is **offline**, it is marked entirely offline; all outlets are unavailable.

6. **Device Dependencies (Advanced Power Sequencing)**  
   - Users can define **startup/shutdown order** by specifying that “Device B depends on Device A” with a **custom time delay**.  
   - **Circular dependencies** are blocked by the app.  
   - If the dependency device is unreachable or removed, the dependent device will **still proceed** with its action (the dependency is ignored).  
   - **Scheduling overrides dependencies**—if a device’s schedule says to turn on now, it turns on even if its dependency is off.

7. **Status Monitoring**  
   - The app **pings each device every 60 seconds**.  
   - A device is marked offline if it fails **5 consecutive pings** (~5 minutes of no response).  
   - Once it responds again, it is immediately marked online.  
   - **No advanced discovery**—all device details (IP, MAC) are maintained manually by the user.

8. **Error Handling & Logging**  
   - If a device **fails** to execute a command but is still ping-responsive, the app **retries once** after a short delay.  
   - If the device is **unreachable**, scheduled actions are **skipped** and logged as failures.  
   - Failures are recorded in a **debug or error log** (`C:\MuseumControl\debug.log`) and shown in a dedicated **Error Log** tab in the UI.  
   - Errors are also shown briefly as **notifications** (e.g., “Failed to turn off Projector-1”).

9. **Application Behavior**  
   - The app **auto-starts with Windows**, minimizing to the system tray (users can reopen via tray icon).  
   - **No user authentication**—anyone on the network can access the UI, and everyone has full control.  
   - The app checks a **specific GitHub repository** for updates on startup, prompting the user to install if a new version is available.  
   - **No device archiving**—devices are either active in the JSON file or fully deleted.

10. **GUI & Dashboard**  
   - **Main Screen (Dashboard)**:  
     - High-level overview of **today’s scheduled actions** (start/end time + schedule category).  
     - **Global scheduler** on/off toggle.  
     - **Main On/Off button** controlling all devices in the **“Main Mediatechnik Preset.”**  
     - Quick overview of **groups/presets** for turning each group on/off.  
     - Live list of **Offline Devices** in a side panel/tab.  
     - **No** individual device tiles on the main screen; for device-specific views, navigate to the next point.  
   - **Exhibition / Areas**:  
     - Tabs for **area/cluster** (e.g., Democracy, Mobility) and for **device categories** (e.g., PCs, Projectors).  
     - Users see **device tiles** with statuses (online/offline) only in these area/category sub-tabs. Clicking a tile opens control actions.  
   - **Scheduler**:  
     - A **list view** of upcoming actions, grouped by device or time.  
     - Users can add new schedules (weekly or one-time) for devices or device groups.  
     - Schedules are auto-saved to the JSON.  
   - **Device Management**:  
     - Add/edit/remove devices.  
     - Must assign at least one scheduler group if automation is needed, but category/area is optional on creation.  
     - Support for **renaming** devices and specifying custom commands.  
   - **Settings**:  
     - **Export/Import** configuration to/from a JSON file for backup/restoration.  
     - **Projector commands** can be customized or chosen from a brand list stored in a separate JSON (if applicable).  
     - Language selection: **English/German** manually chosen (no auto-detect).  
     - Debug logs (error logs).  

---

## **3. Data Handling Details**

- **`devices.json`**:  
  - Stores an array of device objects, each containing:
    ```json
    {
      "name": "PC-1",
      "type": "pc",
      "ip": "192.168.x.x",
      "mac": "00:1A:2B:3C:4D:5F",
      "area": "Democracy",
      "category": "PCs",
      "schedulerGroups": ["Main Mediatechnik Preset", "MorningStartup"],
      "commands": {
        "on": "wakeonlan",
        "off": "shutdown -s -t 0"
      },
      "dependencies": [
        {
          "dependsOn": "Projector-1",
          "delayMinutes": 3
        }
      ],
      "schedule": [
        {
          "action": "turn_on",
          "time": "09:00",
          "days": ["Monday", "Tuesday", "Wednesday"]
        },
        {
          "action": "turn_off",
          "time": "18:00",
          "days": ["Monday", "Wednesday"]
        }
      ]
    }
    ```
- **`projectors.json`** (optional):  
  - Stores known commands for projector brands:
    ```json
    {
      "projector_brands": {
        "Epson": {
          "power_on": "...",
          "power_off": "..."
        },
        "Panasonic": {
          "power_on": "...",
          "power_off": "..."
        }
      }
    }
    ```
- **`powerstrips.json`** (optional if multiple power strip models require different commands).

- **Changes** to these JSON files occur **automatically** upon any UI edits (auto-save).  
- **Import/Export**: Users can export the entire configuration to a single JSON file (merging or copying `devices.json` and other files) and import it later. During import, a **preview** shows the changes.

---

## **4. Architecture**

1. **Backend**  
   - Runs on a **central Windows PC** (auto-start with Windows).  
   - Periodically **pings** devices (every 60 seconds).  
   - Sends device commands (WoL, shutdown, projector commands, etc.).  
   - Provides an **API** or local interface for the frontend.

2. **Frontend / UI**  
   - A **modern, visual UI** (could be a web-based dashboard or a Windows-based GUI) that interacts with the backend.  
   - **Minimizes** to the **system tray**; user can open the main window from the tray icon.  
   - **All data** is pulled from JSON and displayed in real time.

3. **Auto-Update Mechanism**  
   - On startup, the app **checks a designated GitHub repo** for newer releases.  
   - If an update is found, the user is **prompted** to install.

---

## **5. Error Handling Strategy**

1. **Command Failures**  
   - If the device is **online (pingable)** but a command fails, the app **retries once** after a short delay (~5 seconds).  
   - If it fails again, the action is **logged** as a failure; the device’s schedule or future actions are **not** halted.

2. **Offline / Unreachable Devices**  
   - A device is marked **offline** after **5 consecutive ping failures** (60-second ping interval → ~5 minutes).  
   - Scheduled actions for an offline device are **skipped** (no retries later).  
   - Once the device responds to pings, it’s **instantly marked online**.

3. **Dependencies**  
   - If a dependency device is **offline or removed**, the dependent device will **still proceed** with its action.  
   - Any failures are **logged**, but do not block other devices.

4. **Error Log**  
   - A **debug/error log** (`debug.log`) stores timestamps and failure reasons.  
   - The UI has an **Error Log tab** listing recent errors.  
   - **Brief notifications** appear in the UI (e.g., “Failed to turn off PC-2”).

---

## **6. Testing Plan**

1. **Unit Tests**  
   - **Device Management**: Adding/removing/editing devices, verifying JSON updates.  
   - **Group Assignments**: Ensuring correct group membership, correct on/off actions for groups.  
   - **Scheduler**: Testing weekly vs. one-time schedules, ensuring last action wins in conflicts.  

2. **Integration Tests**  
   - **Ping & Online/Offline** Detection: Simulate devices going offline/online, verify correct status changes.  
   - **Power Strip Control**: Test turning individual outlets on/off, skipping if the strip is offline.  
   - **Wake-on-LAN** & **Projector Commands**: Validate correct command dispatch and error handling.  

3. **Performance Tests**  
   - Scale up to **200 devices**, verifying ping intervals remain stable (60-second polling).  
   - Ensure UI remains responsive with large device lists.  

4. **UI/UX Tests**  
   - Confirm **main dashboard** shows high-level schedule overview, group/preset toggles, and offline devices.  
   - Verify correct behavior for manual overrides vs. scheduled actions.  
   - Check **import/export** functionality with preview.  

5. **User Acceptance Testing (UAT)**  
   - Have real users or museum staff test typical scenarios: scheduling open/close times, manually turning devices on/off, verifying offline notifications, etc.  
   - Confirm the system meets day-to-day operational needs (no role-based restrictions, simple no-validation input, etc.).

---

## **7. Final Notes & Developer Handover**

- **No advanced user roles**: Everyone on the network has full access.
- **No archiving**: Devices are either active in JSON or deleted entirely.
- **Schedules are never blocked** by manual actions; the app always proceeds with automation.
- **English & German** are supported; user picks manually.

With these requirements in place, a developer should have all the information needed to implement the application. Any additional **edge cases** or **minor tweaks** can be addressed during development, but this specification covers the primary functionality and design decisions agreed upon.

**End of spec.md**