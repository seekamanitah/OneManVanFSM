# Version Tracking & Development Workflow Guide
## OneManVanFSM Mobile App

---

## 📋 Overview

This guide explains how to track app versions, know when to rebuild, and optimize your development workflow.

---

## 🔢 Version Numbering System

### **Display Version** (User-Facing)
- **Format**: `1.0.0` (MAJOR.MINOR.PATCH)
- **Location**: `ApplicationDisplayVersion` in `.csproj`
- **Shown In**: Settings page, About section
- **When to Change**: Manually update for releases

### **Build Number** (Auto-Generated)
- **Format**: `DDDDHHMM` (Days since 2025-01-01 + Time)
- **Example**: `461713` = 46 days + 17:13 (5:13 PM)
- **Location**: `ApplicationVersion` in `.csproj` (auto-calculated on build)
- **Shown In**: Settings page, Home page (if > 24 hours old)
- **Updates**: Automatically on every build

### **Informational Version** (Internal)
- **Format**: `2025-02-15-1713-Debug`
- **Location**: `InformationalVersion` in `.csproj`
- **Purpose**: Full build timestamp + configuration

---

## 📱 Checking Your Installed Version

### **On Device:**
1. Open the app
2. Navigate to **Settings** (bottom nav)
3. Scroll to **"About"** section
4. Look at:
   - **Version**: 1.0.0
   - **Build Number**: 461713 (Feb 15, 2025 5:13 PM)

### **Home Page Warning:**
If your build is **> 24 hours old**, you'll see a yellow warning banner:
```
⚠️ Running build from Feb 14 5:13 PM (28 hours old). Check Settings for version info
```

---

## 🔨 When to Rebuild APK

### ✅ **MUST Rebuild** (Full APK + Reinstall)

1. **Native Code Changes**
   - Changes to `Platforms/Android/`, `Platforms/Windows/`
   - AndroidManifest.xml modifications
   - Native library additions (NuGet packages with native dependencies)

2. **Project Configuration**
   - `.csproj` changes (new NuGet packages, target frameworks)
   - Version number changes
   - Build configuration changes (Debug → Release)

3. **New Files/Assets**
   - Added images, fonts, resources
   - New `.razor` component files (first time)

4. **Database Schema Changes**
   - Changes to `AppDbContext.cs` models (if using migrations)
   - If you see `FormatException` or schema errors

5. **Startup/Dependency Changes**
   - `MauiProgram.cs` modifications
   - Service registration changes (DI container)

### 🔥 **Hot Reload Only** (No Rebuild Needed)

1. **Razor Markup Changes**
   - `.razor` file HTML/markup updates
   - CSS inline styles
   - Bindings (`@bind`, `@onclick`)

2. **C# Logic in Existing Methods**
   - Method body changes in `@code` blocks
   - Variable value changes
   - Conditional logic updates

3. **Service Implementation**
   - Changes to existing service method bodies
   - Business logic updates

**How to Apply:**
- Press **Ctrl+Alt+F5** in Visual Studio
- Or use the 🔥 Hot Reload button in toolbar
- Changes apply **instantly** without rebuild!

### ⚠️ **Rebuild Recommended** (Not Required, But Safer)

1. **Large Refactoring**
   - Multiple file changes
   - Method signature changes

2. **After 10+ Hot Reloads**
   - Hot Reload can sometimes drift
   - Fresh build ensures clean state

---

## 🚀 Optimal Development Workflow

### **1. Initial Setup (Once)**
```bash
# Build and deploy to device
dotnet publish OneManVanFSM.csproj -f net10.0-android -c Debug -r android-arm64 /p:AndroidPackageFormat=apk
adb install -r bin\Debug\net10.0-android\android-arm64\publish\com.companyname.onemanvanfsm-Signed.apk
```

### **2. During Active Development**
```
1. Make code changes (Razor, Services, etc.)
2. Press Ctrl+Alt+F5 (Hot Reload)
3. Test on device instantly
4. Repeat 1-3 as needed
```

### **3. When Hot Reload Fails**
```
# Symptom: Changes don't appear OR app crashes
# Solution: Full rebuild
F5 (Stop) → Shift+F5 (Restart Debugging)
```

### **4. End of Session / Before Field Test**
```bash
# Build fresh Release APK
dotnet publish OneManVanFSM.csproj -f net10.0-android -c Release -r android-arm64 /p:AndroidPackageFormat=apk

# Note the build number:
# Settings → About → Build Number: 461713 (Feb 15, 2025 5:13 PM)
```

---

## 📊 Version Comparison

### **Checking Development vs Device**

#### **Current Code (Visual Studio):**
```csharp
// OneManVanFSM.csproj line 27-29
<ApplicationDisplayVersion>1.0.0</ApplicationDisplayVersion>
<ApplicationVersion>$([System.Math]::Floor(...)).ToString('F0'))$([System.DateTime]::UtcNow.ToString('HHmm'))</ApplicationVersion>
```
- Build number calculated **at build time**
- If you build now: `461713` (46 days + 17:13)
- If you build tomorrow: `471345` (47 days + 13:45)

#### **Device Build Number:**
- Open app → Settings → About
- Compare device build number to Git commit
- If different by > 24 hours, warning appears on Home page

---

## 🔍 Troubleshooting

### **"I don't know if I need to rebuild"**

**Check:**
1. Did you change `.csproj`, `MauiProgram.cs`, or native code? → **Rebuild**
2. Did you add new files (`.razor`, images)? → **Rebuild**
3. Only changed existing `.razor` markup or C# logic? → **Hot Reload**
4. Not sure? → **Rebuild** (safer)

### **"Hot Reload says 'Applied' but I don't see changes"**

**Try:**
1. Stop debugging (Shift+F5)
2. Clean build: `dotnet clean`
3. Rebuild: F5
4. If still broken, delete `bin/` and `obj/` folders

### **"Build number not showing correctly"**

**Verify:**
```bash
# Check what the build system calculates
dotnet build -v:m | findstr "ApplicationVersion"
```

Should output something like: `ApplicationVersion = 461713`

### **"App crashes immediately after install"**

**Likely causes:**
1. Mixed Debug/Release builds
2. Old `.apk` cached - uninstall completely first
3. Native library mismatch

**Solution:**
```bash
adb uninstall com.companyname.onemanvanfsm
# Then reinstall fresh APK
```

---

## 📝 Git Workflow Integration

### **Tagging Releases**
```bash
# When deploying to production/field test
git tag -a v1.0.0-build461713 -m "Production release Feb 15"
git push origin v1.0.0-build461713
```

### **Branch Strategy**
- `develop` → Daily development builds
- `main` → Production-ready builds only
- Feature branches → Hot Reload testing

---

## ⏱️ Build Time Estimates

| Change Type | Hot Reload | Full Debug Build | Release APK |
|-------------|------------|------------------|-------------|
| Razor markup | < 1 sec | - | - |
| Service logic | < 1 sec | - | - |
| New file | - | ~30 sec | ~2 min |
| Native change | - | ~45 sec | ~3 min |
| Clean + Rebuild | - | ~60 sec | ~5 min |

**Tip:** Use Hot Reload 90% of the time to stay productive!

---

## 🎯 Summary Decision Tree

```
Did I change anything?
├─ No → No action needed
├─ Yes → What changed?
    ├─ Markup/CSS/Logic only → Hot Reload (Ctrl+Alt+F5)
    ├─ Added new file → Full Rebuild (F5)
    ├─ Changed .csproj → Full Rebuild
    ├─ Changed MauiProgram.cs → Full Rebuild
    ├─ Changed native code → Full Rebuild + Reinstall APK
    └─ Not sure → Full Rebuild (safer)
```

---

## 🔗 Quick Reference

| Task | Command/Shortcut |
|------|------------------|
| Hot Reload | `Ctrl+Alt+F5` |
| Stop Debugging | `Shift+F5` |
| Start Debugging | `F5` |
| Build Release APK | `dotnet publish ... -c Release` |
| Check Build Number | Settings → About |
| View Build Age | Home page (if > 24h) |
| Uninstall App | `adb uninstall com.companyname.onemanvanfsm` |
| Install APK | `adb install -r path/to/app.apk` |

---

**Last Updated:** Feb 15, 2025  
**Version System Implemented:** Build 461713
