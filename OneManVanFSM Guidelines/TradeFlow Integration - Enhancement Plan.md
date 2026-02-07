# OneManVanFSM Enhancement Plan: TradeFlow FSM Integration
# Refined Analysis — Fields & Features to Add

> **Source**: TradeFlow FSM Comprehensive Vision document compared against:
> - All 19 current data models in `OneManVanFSM.Shared/Models/`
> - All current guideline files in `OneManVanFSM Guidelines/`
>
> **Scope**: Identify concrete, implementable additions to data models, relationships,
> and missing capabilities. HVAC is primary focus; plumbing/electrical secondary.
>
> **Rule**: No code changes in this document. Review plan only.

---

## KEY FINDING SUMMARY

The current guideline `.txt` files are **already more detailed** than TradeFlow FSM
in most workflow areas (estimates, jobs, material lists, scheduling, financials).
The guidelines describe rich interconnections and UI flows that surpass TradeFlow.

**The actual gaps are in the DATA MODELS** — fields and relationships described in
both TradeFlow AND your own guidelines that haven't been added to the C# model
classes yet. This plan focuses on those concrete model-level additions.

---

## SECTION 1: ASSET MODEL ENHANCEMENTS (Highest Impact)

The Asset model has the most significant gaps. Both TradeFlow and your own Assets
guideline describe fields that don't exist in the current `Asset.cs` model.

### 1A. HVAC-Critical Fields (HIGH Priority)

These are fields a tech needs when looking at a data plate on-site:

| Field | Type | Why It Matters |
|---|---|---|
| `Brand` | string? | Separate from Model. "Carrier", "Lennox", "Trane" — critical for warranty claims, parts compatibility, and customer preferences. Your guideline mentions "Preferred Equipment Brand" on customers. |
| `FuelType` | string? (dropdown) | Natural Gas / Propane / Electric / Oil / Dual Fuel. TradeFlow: core HVAC enum. Your guideline: "refrigerant compliance" but no fuel field exists. Drives safety notes ("Propane: Verify Tank"). |
| `UnitConfiguration` | string? (dropdown) | Split / Packaged / Furnace / Coil / Condenser / Mini-Split / Ductless / Heat Pump. TradeFlow: core enum. Your guideline describes "system type" on jobs but not on assets. |
| `BTURating` | int? | BTU capacity (24000, 48000, 80000). TradeFlow: critical for sizing/compatibility. Your guideline mentions BTU in inventory compatibility but not on assets. |
| `FilterSize` | string? | "16x25x1", "20x20x4". Most common field a tech needs on a service call. Not in TradeFlow or guidelines but is a standard HVAC data point. |
| `WarrantyStartDate` | DateTime? | Currently only `WarrantyExpiry` exists. Both TradeFlow and guidelines describe warranty calc from start + term. Need start date to properly calculate and display. |
| `WarrantyTermYears` | int? | Auto-calc expiry from start + term. TradeFlow: core field. |

### 1B. Efficiency & Compliance Fields (MEDIUM Priority)

| Field | Type | Why It Matters |
|---|---|---|
| `AFUE` | decimal? | Annual Fuel Utilization Efficiency (%) — furnaces. Industry standard rating. |
| `HSPF` | decimal? | Heating Seasonal Performance Factor — heat pumps. |
| `Voltage` | string? (dropdown) | 120V / 208V / 240V / 480V. Needed for electrical compatibility, also critical when sizing HVAC disconnects. |
| `Phase` | string? (dropdown) | Single Phase / Three Phase. Commercial units require this. |
| `LocationOnSite` | string? | "Basement", "Attic", "Closet", "Roof", "Side Yard". Your guideline mentions `EquipmentLocation` on Sites, but per-asset location is different (one site may have furnace in basement AND condenser on side yard). |
| `LastServiceDate` | DateTime? | Quick reference without querying full service history. |
| `NextServiceDue` | DateTime? | Drives scheduling alerts. Both TradeFlow and guidelines describe warranty/maintenance alerts. |
| `ManufactureDate` | DateTime? | Often on data plate, different from install date. Useful for age-based recommendations. |

### 1C. Multi-Trade Extension Fields (LOW-MEDIUM Priority)

These make the asset model work for plumbing and electrical too:

| Field | Type | Trade |
|---|---|---|
| `AmpRating` | int? | Electrical — panel/circuit amp rating |
| `PanelType` | string? (dropdown) | Electrical — Main Breaker / Main Lug / Sub-panel |
| `PipeMaterial` | string? (dropdown) | Plumbing — Copper / PEX / PVC / Cast Iron |
| `GallonCapacity` | int? | Plumbing — water heater tank size |

### 1D. Asset Relationship Gap: Job ? Asset (HIGH Priority)

**Both TradeFlow AND your own guidelines** describe a many-to-many Job?Asset
relationship (e.g., "which assets were serviced on which job", "auto-log against
assets", "Job 'Link Asset' pulls from site/customer").

Your Jobs guideline says:
> "Many-to-Many (via JobAssets junction or material lists)"

But **no `JobAsset` join table exists** in the current models. This is the single
most important missing relationship — it connects service history to specific
equipment.

**Proposed: `JobAsset` model**
```
JobAsset { Id, JobId, AssetId, Role (Serviced/Installed/Replaced/Inspected), Notes }
```

---

## SECTION 2: SITE MODEL ENHANCEMENTS

Your current Site model is already strong (SqFt, Zones, Stories, AccessCodes,
Instructions, Parking, EquipmentLocation). But both TradeFlow and guidelines
describe safety-critical fields missing from the model:

### 2A. Safety & Access Fields (HIGH for HVAC)

| Field | Type | Why It Matters |
|---|---|---|
| `GasLineLocation` | string? | Where the gas shutoff is. TradeFlow: "Propane: Verify Tank" safety note. Your guideline: "Gas Line Shutoff?" prompt. Not in model. |
| `ElectricalPanelLocation` | string? | Where the main panel is. Important for electrical trade and HVAC disconnects. |
| `WaterShutoffLocation` | string? | Main water shutoff. Important for plumbing trade. |
| `HeatingFuelSource` | string? (dropdown) | Natural Gas / Propane / Electric / Oil. Site-level default fuel. TradeFlow: "fuel enums". Different from asset-level FuelType — this is what the property is piped for. |
| `YearBuilt` | int? | Impacts code requirements, insulation assumptions, duct material expectations. Both documents mention building age. |

### 2B. Convenience Fields (MEDIUM)

| Field | Type | Why It Matters |
|---|---|---|
| `HasAtticAccess` | bool? | Quick field for HVAC techs (air handlers, ductwork often in attic). |
| `HasCrawlSpace` | bool? | Affects ductwork routing, plumbing access. |
| `HasBasement` | bool? | Common equipment location. |

---

## SECTION 3: JOB MODEL ENHANCEMENTS

### 3A. Categorization Fields (HIGH Priority)

| Field | Type | Why It Matters |
|---|---|---|
| `TradeType` | string? (dropdown) | HVAC / Plumbing / Electrical / General. Your app supports all three trades but jobs have no way to categorize which trade the work belongs to. Critical for reporting, filtering, and tech assignment by certification. |
| `JobType` | string? (dropdown) | Install / Repair / Maintenance / Diagnostic / Inspection / Tune-Up / Replacement. TradeFlow describes this. Your guideline mentions "Lead/Quoted/Scheduled" as status but no work-type categorization. |

### 3B. Execution Tracking (MEDIUM)

| Field | Type | Why It Matters |
|---|---|---|
| `ActualDuration` | decimal? | Actual hours spent vs. `EstimatedDuration`. Can be computed from TimeEntries but a stored field allows quick comparison without joins. |
| `ActualTotal` | decimal? | Actual cost vs. `EstimatedTotal`. Same rationale. |
| `PermitRequired` | bool? | Flag if a permit is needed. Your guideline mentions permits in material lists and financials. |
| `PermitNumber` | string? | Track permit reference number. |

### 3C. Missing Navigation Collections

The Job model is missing some nav collections for FKs that exist on other models:

| Missing Nav | Already Has FK On | Impact |
|---|---|---|
| `ICollection<Expense> Expenses` | Expense.JobId | Can't navigate Job?Expenses without it |
| `ICollection<Document> Documents` | Document.JobId | Can't navigate Job?Documents without it |
| `ICollection<JobAsset> JobAssets` | (new table) | The proposed join table from Section 1D |

---

## SECTION 4: ESTIMATE & INVOICE LINE ENHANCEMENTS

### 4A. EstimateLine Improvements (MEDIUM-HIGH)

| Field | Type | Why It Matters |
|---|---|---|
| `LineType` | string? (dropdown) | Labor / Material / Equipment / Fee / Discount. Currently all lines look the same — can't distinguish labor from materials in totals or reports. Both TradeFlow and your Material Lists guideline describe section-based categorization. |
| `Unit` | string? | "Each", "Hour", "Ft", "Box", "Roll". MaterialListItem already has Unit but EstimateLine doesn't. |
| `Section` | string? | Grouping within estimate. MaterialListItem has this as a required field but EstimateLine doesn't. |

### 4B. InvoiceLine Improvements (Same as EstimateLine)

| Field | Type | Why It Matters |
|---|---|---|
| `LineType` | string? (dropdown) | Same as above — Labor / Material / Equipment / Fee / Discount. |
| `Unit` | string? | Same as above. |

### 4C. Estimate Model Additions (MEDIUM)

| Field | Type | Why It Matters |
|---|---|---|
| `TradeType` | string? (dropdown) | HVAC / Plumbing / Electrical — same as jobs, for reporting. |
| `VersionNumber` | int | Your guideline explicitly mentions "Revisions & History Accordion: Version table". TradeFlow describes "version history for revisions". |
| `DepositRequired` | decimal? | Deposit amount or percentage. Your financials guideline mentions deposits. |
| `DepositReceived` | bool? | Whether deposit collected before work begins. |

### 4D. Invoice Model Additions (MEDIUM)

| Field | Type | Why It Matters |
|---|---|---|
| `InvoiceDate` | DateTime | When invoice was issued (distinct from CreatedAt). Standard accounting field. |
| `PaymentTerms` | string? | "Net 30", "Due on Receipt", etc. |
| `DiscountAmount` | decimal? | Applied discount. |
| `DepositApplied` | decimal? | Deposit credited against total. |

---

## SECTION 5: PRODUCT & INVENTORY ENHANCEMENTS

### 5A. Product Model (MEDIUM-HIGH)

| Field | Type | Why It Matters |
|---|---|---|
| `Brand` | string? | Manufacturer brand separate from name. "Carrier", "Honeywell", etc. |
| `PartNumber` | string? | Manufacturer part number / SKU. Critical for ordering. |
| `Barcode` | string? | For mobile scanning (your guideline mentions barcode scanning). |
| `TradeCategory` | string? (dropdown) | HVAC / Plumbing / Electrical subcategory. |
| `ImagePath` | string? | Product photo for quick identification. |
| `IsActive` | bool | Soft-deactivate without full archive. Currently only IsArchived exists. |

### 5B. InventoryItem Model (MEDIUM)

| Field | Type | Why It Matters |
|---|---|---|
| `SKU` | string? | Part number for ordering/scanning. |
| `Barcode` | string? | For mobile scanning. Your guideline: "Barcode scan for add/deduct". |
| `ShelfBin` | string? | Physical location within warehouse/truck. "Shelf B3", "Truck Drawer 2". |
| `PreferredSupplier` | string? | Default vendor for reorders. |
| `LastRestockedDate` | DateTime? | When last received/restocked. |

---

## SECTION 6: EMPLOYEE MODEL ENHANCEMENTS

### 6A. Licensing & Compliance (HIGH for HVAC)

| Field | Type | Why It Matters |
|---|---|---|
| `LicenseNumber` | string? | Trade license (EPA 608, state HVAC license, journeyman electrician). Your guideline mentions "EPA 608: Expires 2027" in certs JSON but a dedicated license field is clearer for the primary license. |
| `LicenseExpiry` | DateTime? | Track renewal date. Your guideline: "certification expirations auto-flag unschedulable techs". |
| `EmergencyContactName` | string? | Safety requirement for field workers. |
| `EmergencyContactPhone` | string? | Safety requirement. |
| `VehicleAssigned` | string? | Truck number/plate. Your guideline mentions "Vehicle Assigned" as a customizable field. Links to inventory location tracking. |
| `OvertimeRate` | decimal? | Separate from HourlyRate. Your guideline mentions "overtime calcs for long days". |

---

## SECTION 7: TIME ENTRY ENHANCEMENTS

### 7A. Time Categorization (HIGH)

| Field | Type | Why It Matters |
|---|---|---|
| `TimeCategory` | string? (dropdown) | Travel / On-Site / Diagnostic / Admin / Break. TradeFlow: "Travel vs. On-Site" breakdown. Your guideline mentions labor categorization for payroll. Without this, all time looks the same — can't distinguish billable on-site work from drive time. |
| `AssetId` | int? (FK) | Log time against a specific asset. TradeFlow: "2 hours on Serial #XYZ". Ties service time to equipment for maintenance cost analysis. |

---

## SECTION 8: CALENDAR EVENT ENHANCEMENTS

### 8A. Event Classification (MEDIUM)

| Field | Type | Why It Matters |
|---|---|---|
| `EventType` | string? (dropdown) | Job / Personal / Meeting / Reminder / Block-Off. Currently all events look the same on the calendar. Your guideline describes color-coding by type. |
| `IsRecurring` | bool | Flag for recurring events. Your guideline: "agreements auto-populate recurring events". |
| `RecurrenceRule` | string? | iCal RRULE format. Enables proper recurring scheduling for service agreements. |
| `Color` | string? | Hex color for visual categorization on calendar. |

---

## SECTION 9: SERVICE AGREEMENT ENHANCEMENTS

### 9A. Renewal & Coverage (MEDIUM)

| Field | Type | Why It Matters |
|---|---|---|
| `RenewalDate` | DateTime? | When auto-renewal triggers. Your guideline: "alert 30 days before expiry". |
| `AutoRenew` | bool | Auto-renew flag. |
| `TradeType` | string? (dropdown) | HVAC / Plumbing / Electrical — what trade the agreement covers. |
| `BillingFrequency` | string? (dropdown) | Monthly / Quarterly / Annual. Your guideline: "monthly billing for multi-site landlords". |
| `DiscountPercent` | decimal? | Service agreement customer discount on work. |

### 9B. Missing Relationship: Agreement ? Asset (MEDIUM-HIGH)

Your guideline explicitly states:
> "Many-to-Many (via AgreementAssets junction)"

But no `ServiceAgreementAsset` join table exists. This would track which specific
assets an agreement covers (e.g., "Covers the furnace and AC at Site A, but not
the water heater").

**Proposed: `ServiceAgreementAsset` model**
```
ServiceAgreementAsset { Id, ServiceAgreementId, AssetId, Notes }
```

---

## SECTION 10: CUSTOMER MODEL ENHANCEMENTS

### 10A. Contact & Preference Fields (MEDIUM)

| Field | Type | Why It Matters |
|---|---|---|
| `SecondaryPhone` | string? | Alt contact (cell vs. home). Common for residential customers. |
| `PreferredContactMethod` | string? (dropdown) | Phone / Email / Text. Saves time when reaching out. |
| `ReferralSource` | string? (dropdown) | How they found you — word-of-mouth, Google, Angi, etc. Lead tracking for marketing. |
| `Tags` | string? (JSON) | Quick categorization: "VIP", "Warranty Customer", "Propane". Your guideline mentions filtering by customer type but tags allow more flexible grouping. |

---

## SECTION 11: NEW ENTITIES (Not in current app)

### 11A. JobAsset Join Table (HIGH — discussed in Section 1D)

```
JobAsset
??? Id (PK)
??? JobId (FK ? Job)
??? AssetId (FK ? Asset)
??? Role: string? (dropdown: Serviced / Installed / Replaced / Inspected / Diagnosed)
??? Notes: string?
??? CreatedAt: DateTime
```

**Why**: Links specific equipment to specific jobs. Enables per-asset service history,
time tracking per asset, and "which assets were touched on this job" reporting.

### 11B. ServiceAgreementAsset Join Table (MEDIUM — discussed in Section 9B)

```
ServiceAgreementAsset
??? Id (PK)
??? ServiceAgreementId (FK ? ServiceAgreement)
??? AssetId (FK ? Asset)
??? CoverageNotes: string?
??? CreatedAt: DateTime
```

**Why**: Tracks which specific assets an agreement covers.

### 11C. AssetServiceLog (MEDIUM)

```
AssetServiceLog
??? Id (PK)
??? AssetId (FK ? Asset)
??? ServiceType: string (dropdown: Filter Change / Refrigerant Charge / Cleaning / Calibration / Inspection)
??? ServiceDate: DateTime
??? PerformedBy: string?
??? Notes: string?
??? NextDueDate: DateTime?
??? Cost: decimal?
??? CreatedAt: DateTime
```

**Why**: Quick maintenance log without creating a full Job or ServiceHistoryRecord.
TradeFlow describes "Filter Change History" table. Enables tracking routine
maintenance like filter changes that don't warrant a full job record.

### 11D. Supplier Entity (LOW-MEDIUM)

```
Supplier
??? Id (PK)
??? Name: string
??? ContactName: string?
??? Phone: string?
??? Email: string?
??? Website: string?
??? AccountNumber: string?
??? PaymentTerms: string?
??? Notes: string?
??? IsActive: bool
??? CreatedAt: DateTime
```

**Why**: Both TradeFlow and your Product/Inventory guidelines mention supplier
tracking. Currently Product has `SupplierName` as a string — a dedicated entity
allows proper supplier management and links across products/inventory.

---

## SECTION 12: DROPDOWN OPTIONS TO SEED

These new dropdowns should be added to the `DropdownOption` table for the new
fields above. Using the existing `DropdownOption` pattern (Category/Value/SortOrder):

| Category | Seed Values |
|---|---|
| `FuelType` | Natural Gas, Propane, Electric, Oil, Dual Fuel |
| `UnitConfiguration` | Split System, Packaged Unit, Furnace, Air Handler, Condenser, Mini-Split, Ductless, Heat Pump, Coil, Boiler |
| `Voltage` | 120V, 208V, 240V, 277V, 480V |
| `Phase` | Single Phase, Three Phase |
| `AssetLocation` | Basement, Attic, Closet, Garage, Roof, Side Yard, Crawl Space, Utility Room, Backyard, Mechanical Room |
| `TradeType` | HVAC, Plumbing, Electrical, General |
| `JobType` | Install, Repair, Maintenance, Diagnostic, Inspection, Tune-Up, Replacement, Retrofit, Emergency |
| `LineType` | Labor, Material, Equipment, Fee, Discount, Permit, Disposal |
| `TimeCategory` | Travel, On-Site, Diagnostic, Admin, Break, Training |
| `HeatingFuelSource` | Natural Gas, Propane, Electric, Oil, Geothermal, Solar |
| `FilterSize` | 14x20x1, 14x25x1, 16x20x1, 16x25x1, 20x20x1, 20x25x1, 16x25x4, 20x20x4, 20x25x4, Custom |
| `EventType` | Job, Personal, Meeting, Reminder, Block-Off, Training |
| `BillingFrequency` | Monthly, Quarterly, Semi-Annual, Annual |
| `ReferralSource` | Word of Mouth, Google, Angi, HomeAdvisor, Facebook, Yard Sign, Repeat Customer, Other |
| `PreferredContact` | Phone, Email, Text, No Preference |
| `PipeMaterial` | Copper, PEX, PVC, CPVC, Cast Iron, Galvanized, ABS |
| `PanelType` | Main Breaker, Main Lug, Sub-Panel |
| `AssetServiceType` | Filter Change, Refrigerant Charge, Coil Cleaning, Calibration, Inspection, Drain Flush, Belt Replacement |
| `JobAssetRole` | Serviced, Installed, Replaced, Inspected, Diagnosed, Decommissioned |

---

## IMPLEMENTATION PRIORITY TIERS

### TIER 1 — Do First (HVAC Data Collection Essentials)
1. Asset: Brand, FuelType, UnitConfiguration, BTURating, FilterSize, WarrantyStartDate, WarrantyTermYears
2. Job: TradeType, JobType
3. JobAsset join table (new entity)
4. Site: GasLineLocation, HeatingFuelSource, YearBuilt
5. TimeEntry: TimeCategory
6. Seed DropdownOptions for all new dropdown fields

### TIER 2 — Do Second (Cross-Trade & Reporting Improvements)
7. Asset: AFUE, HSPF, Voltage, LocationOnSite, LastServiceDate, NextServiceDue
8. EstimateLine/InvoiceLine: LineType, Unit
9. Estimate: TradeType, VersionNumber, DepositRequired
10. Invoice: InvoiceDate, PaymentTerms
11. Product: Brand, PartNumber, Barcode
12. Employee: LicenseNumber, LicenseExpiry, VehicleAssigned
13. Job: Missing nav collections (Expenses, Documents, JobAssets)

### TIER 3 — Do Third (Refinements & New Entities)
14. ServiceAgreement: RenewalDate, AutoRenew, TradeType, BillingFrequency
15. ServiceAgreementAsset join table (new entity)
16. AssetServiceLog (new entity)
17. CalendarEvent: EventType, IsRecurring, RecurrenceRule, Color
18. Customer: SecondaryPhone, PreferredContactMethod, ReferralSource, Tags
19. Site: HasAtticAccess, HasCrawlSpace, HasBasement, ElectricalPanelLocation, WaterShutoffLocation
20. InventoryItem: SKU, Barcode, ShelfBin
21. Asset: Phase, ManufactureDate, AmpRating, PanelType, PipeMaterial, GallonCapacity
22. Employee: EmergencyContactName, EmergencyContactPhone, OvertimeRate

### TIER 4 — Future (When Needed)
23. Supplier entity
24. Job: ActualDuration, ActualTotal, PermitRequired, PermitNumber
25. Invoice: DiscountAmount, DepositApplied
26. Estimate: DepositReceived
27. TimeEntry: AssetId FK, GPS coordinates
28. Customer: TaxExempt, BalanceOwed

---

## NOTES FOR IMPLEMENTATION

1. **All new dropdown fields** should use `string?` type on the model and be populated
   from the `DropdownOption` table via `DropdownService`. This follows the existing
   pattern used for SystemType, Category, etc.

2. **New join tables** (JobAsset, ServiceAgreementAsset) need:
   - Model class in `OneManVanFSM.Shared/Models/`
   - DbSet in `AppDbContext`
   - EF migration
   - Service + interface in `OneManVanFSM.Web/Services/`
   - UI integration on relevant pages

3. **EF Migrations**: Each tier should be a single migration for clean rollback.

4. **UI Impact**: Adding fields to models requires updating the corresponding
   Blazor pages (forms, detail views, list columns) and services (CRUD methods).
   The guideline files already describe where these fields should appear in the UI.

5. **Existing data**: All new fields are nullable (`?`) so existing records are
   unaffected by migration. No data loss risk.

---

> **Next Step**: Review this plan. Confirm which tier(s) to implement first,
> and whether any fields should be added/removed/reprioritized before we begin.
> Once approved, we'll implement tier-by-tier with migrations and UI updates.
