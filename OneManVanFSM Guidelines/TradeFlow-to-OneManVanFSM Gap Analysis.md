# TradeFlow FSM ? OneManVanFSM: Gap Analysis & Enhancement Plan

> **Purpose**: Compare the TradeFlow FSM vision document (old app concept) against the current OneManVanFSM data models and features. Identify missing fields, relationships, and capabilities that would improve data collection — especially for HVAC — while also benefiting plumbing and electrical trades.
>
> **Rule**: No code changes. This is a review document only.

---

## STAGE 1: Data Model Field-by-Field Gap Analysis

### 1. Asset / Equipment Tracking

**Current OneManVanFSM `Asset` model has:**
- Name, Model, SerialNumber, AssetType, Tonnage, SEER, RefrigerantType, RefrigerantQuantity
- InstallDate, WarrantyExpiry, Status (Active/MaintenanceNeeded/Retired/Decommissioned)
- Value, Notes, IsArchived, CreatedAt, UpdatedAt
- FK: ProductId, CustomerId, SiteId

**TradeFlow FSM called for but is MISSING from current app:**

| Missing Field | Type | TradeFlow Description | Trade Focus | Priority |
|---|---|---|---|---|
| **Brand** | string | Separate from Model — e.g., "Carrier", "Lennox", "Trane" | All | HIGH |
| **FuelType** | enum | Natural Gas / Propane / Electric / Oil / Dual Fuel | HVAC | HIGH |
| **UnitConfiguration** | enum | Split / Packaged / Furnace / Coil / Condenser / Mini-Split / Ductless | HVAC | HIGH |
| **BTURating** | int | BTU capacity (e.g., 24000, 48000, 80000) — critical for sizing/compatibility | HVAC | HIGH |
| **WarrantyStartDate** | DateTime | Currently only WarrantyExpiry exists; no start date to calculate from | All | HIGH |
| **WarrantyTermYears** | int | Auto-calc expiry from start + term (e.g., 10 years) | All | MEDIUM |
| **AFUE** | decimal? | Annual Fuel Utilization Efficiency for furnaces (%) | HVAC | MEDIUM |
| **HSPF / HSPF2** | decimal? | Heating Seasonal Performance Factor (heat pumps) | HVAC | MEDIUM |
| **EER** | decimal? | Energy Efficiency Ratio (window units, commercial) | HVAC | LOW |
| **Voltage** | string/enum | 120V / 208V / 240V / 480V — important for electrical compatibility | HVAC/Electrical | MEDIUM |
| **Phase** | string/enum | Single Phase / Three Phase | HVAC/Electrical | LOW |
| **RefrigerantCapacity** | string | Specific charge amount (e.g., "5 lbs 2 oz") — distinct from current RefrigerantQuantity which is decimal | HVAC | MEDIUM |
| **FilterSize** | string | e.g., "16x25x1", "20x20x4" — quick reference for service calls | HVAC | HIGH |
| **FilterChangeHistory** | relation | Sub-table/log of filter replacements with dates | HVAC | MEDIUM |
| **ManufactureDate** | DateTime? | When unit was manufactured (often on data plate, different from install) | All | LOW |
| **LastServiceDate** | DateTime? | Quick reference without querying full service history | All | MEDIUM |
| **NextServiceDue** | DateTime? | Calculated or manual — drives scheduling alerts | All | HIGH |
| **LocationOnSite** | string | "Basement", "Attic", "Closet", "Roof", "Side Yard" — where on the property | All | MEDIUM |
| **PhotoPaths** | string/JSON | Multiple photos of data plates, unit condition, install | All | MEDIUM |
| **ConditionRating** | enum? | Excellent/Good/Fair/Poor — quick field assessment | All | LOW |

**Plumbing-specific fields (extend for multi-trade):**

| Field | Type | Description | Priority |
|---|---|---|---|
| PipeMaterial | string/enum | Copper / PEX / PVC / Cast Iron / Galvanized | MEDIUM |
| PipeDiameter | string | e.g., "3/4 inch", "1 inch" | MEDIUM |
| WaterHeaterType | enum | Tank / Tankless / Heat Pump / Solar | MEDIUM |
| GallonCapacity | int? | Tank size for water heaters | LOW |

**Electrical-specific fields (extend for multi-trade):**

| Field | Type | Description | Priority |
|---|---|---|---|
| AmpRating | int? | Panel or circuit amp rating (100A, 200A, etc.) | MEDIUM |
| PanelType | string/enum | Main Breaker / Main Lug / Sub-panel | LOW |
| CircuitCount | int? | Number of circuits in panel | LOW |

---

### 2. Customer Management

**Current OneManVanFSM `Customer` model has:**
- Name, Type (Individual/Company/Landlord), PrimaryPhone, PrimaryEmail
- Address/City/State/Zip, SinceDate, CreditLimit, Notes, IsArchived
- FK: CompanyId
- Nav: Sites, Jobs, Estimates, Invoices, ServiceAgreements, QuickNotes, Documents

**TradeFlow FSM called for but is MISSING:**

| Missing Field | Type | TradeFlow Description | Priority |
|---|---|---|---|
| **SecondaryPhone** | string? | Alt contact (cell vs. home) | MEDIUM |
| **SecondaryEmail** | string? | Alt email | LOW |
| **PreferredContactMethod** | enum? | Phone / Email / Text / None | MEDIUM |
| **PreferredBrand** | string? | "High-Efficiency Preferred" or brand preference for quotes | HVAC: MEDIUM |
| **ReferralSource** | string? | How they found you (word-of-mouth, Google, etc.) — lead tracking | MEDIUM |
| **TaxExempt** | bool | Tax-exempt flag for commercial/government | LOW |
| **Tags / Labels** | string/JSON | Quick categorization: "VIP", "Warranty Customer", "Propane" | MEDIUM |
| **EmergencyContact** | string? | For multi-family or elderly customers | LOW |
| **CustomerRating** | enum? | Internal rating: A/B/C/D for prioritization | LOW |
| **BalanceOwed** | decimal? | Running balance across invoices — quick snapshot | MEDIUM |

**Missing Relationship / Feature:**
- **Duplicate Merge** — TradeFlow described fuzzy matching on name/address. Currently no merge capability noted.
- **Timeline View** — Aggregated interactions (all jobs, payments, notes in chronological order). This is a UI feature, not a data model gap, but worth noting.

---

### 3. Site Management

**Current OneManVanFSM `Site` model has:**
- Name, Address/City/State/Zip, Latitude/Longitude, PropertyType (Residential/Commercial/Industrial)
- SqFt, Zones, Stories, AccessCodes, Instructions, Parking, EquipmentLocation, Notes
- FK: CustomerId, CompanyId
- Nav: Assets, Jobs, Estimates

**TradeFlow FSM called for but is MISSING:**

| Missing Field | Type | TradeFlow Description | Priority |
|---|---|---|---|
| **GasLineLocation** | string? | "Propane: Verify Tank" safety note — where gas shutoff is | HVAC: HIGH |
| **ElectricalPanelLocation** | string? | Where the main panel is | Electrical: MEDIUM |
| **WaterShutoffLocation** | string? | Main water shutoff | Plumbing: MEDIUM |
| **HasAtticAccess** | bool? | Quick field for HVAC techs | HVAC: MEDIUM |
| **HasCrawlSpace** | bool? | Affects ductwork/plumbing routing | All: LOW |
| **HasBasement** | bool? | Common equipment location | All: LOW |
| **BuildingAge / YearBuilt** | int? | Impacts code requirements and system age estimates | All: MEDIUM |
| **HeatingFuelSource** | enum? | Natural Gas / Propane / Electric / Oil — site-level default | HVAC: HIGH |
| **PhotoPaths** | string/JSON | Photos of the site, access points, equipment areas | All: MEDIUM |
| **SiteInspectionChecklist** | relation | Link to a checklist template for recurring inspections | All: LOW |

---

### 4. Job Management

**Current OneManVanFSM `Job` model has:**
- JobNumber, Title, Description, Status (Lead?Cancelled), Priority (Low?Emergency)
- SystemType, ScheduledDate, ScheduledTime, EstimatedDuration, EstimatedTotal
- Notes, CompletedDate, IsArchived
- FK: CustomerId, CompanyId, SiteId, EstimateId, AssignedEmployeeId, InvoiceId, MaterialListId
- Nav: JobEmployees, TimeEntries, QuickNotes

**TradeFlow FSM called for but is MISSING:**

| Missing Field | Type | TradeFlow Description | Priority |
|---|---|---|---|
| **TradeType** | enum? | HVAC / Plumbing / Electrical / General — categorize the job | HIGH |
| **JobType / WorkType** | enum? | Install / Repair / Maintenance / Diagnostic / Inspection / Tune-Up | HIGH |
| **ActualDuration** | decimal? | Actual hours spent (vs. estimated) — calculated from TimeEntries or manual | MEDIUM |
| **ActualTotal** | decimal? | Actual cost (vs. estimated) | MEDIUM |
| **RequiredTools** | string/JSON | List of tools/equipment needed for the job | MEDIUM |
| **SeasonalTag** | string? | "Fall Furnace Check", "Summer AC Surge" — seasonal categorization | HVAC: LOW |
| **RecurrenceRule** | string? | For recurring jobs (e.g., annual inspections, quarterly maintenance) | MEDIUM |
| **SourceLeadType** | string? | How the lead came in (call, web, referral, service agreement) | MEDIUM |
| **PermitRequired** | bool? | Flag if a permit is needed | All: MEDIUM |
| **PermitNumber** | string? | Track permit reference | All: LOW |
| **InspectionRequired** | bool? | Post-job inspection needed | All: LOW |

**Missing Relationships:**
- **Job ? Asset** (many-to-many) — TradeFlow described auto-logging time against asset serials ("2 hours on Serial #XYZ"). Currently no direct Job?Asset link exists. This is a significant gap for service history per-asset.
- **Job ? Expenses** — Expense model has JobId FK, but Job nav collection doesn't include `ICollection<Expense>`. (Minor nav gap.)
- **Job ? Documents** — No direct Job?Documents nav collection, though Document has JobId FK.

---

### 5. Estimate & Proposal Management

**Current OneManVanFSM `Estimate` model has:**
- EstimateNumber, Title, Status, Priority, SystemType, SqFt, Zones, Stories
- ExpiryDate, PricingMethod, Subtotal, MarkupPercent, TaxPercent, ContingencyPercent, Total
- FK: CustomerId, CompanyId, SiteId, MaterialListId
- Nav: Lines (EstimateLine), Job

**TradeFlow FSM called for but is MISSING:**

| Missing Field | Type | TradeFlow Description | Priority |
|---|---|---|---|
| **TradeType** | enum? | HVAC / Plumbing / Electrical — which trade the estimate covers | HIGH |
| **VersionNumber** | int | Revision tracking — TradeFlow described "version history for revisions" | MEDIUM |
| **PreviousVersionId** | int? | FK to prior version for audit trail | LOW |
| **DisclaimerText** | string? | Standard disclaimers included in PDF output | MEDIUM |
| **UpsellSuggestions** | string/JSON? | Auto-suggested add-ons based on asset history | LOW (future) |
| **DepositRequired** | decimal? | Deposit amount or percentage required before work | MEDIUM |
| **DepositReceived** | bool? | Whether deposit has been collected | MEDIUM |

**EstimateLine is missing:**
- **LineType** (enum: Labor / Material / Equipment / Fee / Discount) — to distinguish line categories
- **Unit** (string) — "Each", "Hour", "Ft", etc.
- **Section** (string) — Grouping within estimate (like MaterialListItem has)

---

### 6. Inventory & Parts

**Current OneManVanFSM `InventoryItem` model has:**
- Name, Location (Warehouse/Truck/Site/Other), Quantity, MinThreshold, MaxCapacity
- LotNumber, ExpiryDate, Cost, Price, MarkupPercent, Notes
- FK: ProductId

**TradeFlow FSM called for but is MISSING:**

| Missing Field | Type | TradeFlow Description | Priority |
|---|---|---|---|
| **SKU / PartNumber** | string? | Manufacturer part number for ordering | HIGH |
| **Barcode** | string? | For mobile scanning | MEDIUM |
| **ShelfBin** | string? | Physical location within warehouse/truck (e.g., "Shelf B3") | MEDIUM |
| **CompatibilityTags** | string/JSON? | "Fits 24k BTU", "R-410A only" — compatibility matrix | HVAC: MEDIUM |
| **ReorderPoint** | decimal? | Alias for MinThreshold but with auto-alert integration | Already exists as MinThreshold |
| **PreferredSupplier** | string? | Default vendor for reorders | MEDIUM |
| **LastRestockedDate** | DateTime? | When last received | LOW |

**Product model is missing:**
- **Brand** (string) — Manufacturer/brand separate from name
- **PartNumber / SKU** (string) — Manufacturer reference
- **Barcode** (string) — For scanning
- **TradeCategory** (enum?) — HVAC / Plumbing / Electrical subcategory
- **CompatibilityMatrix** (string/JSON?) — What units/configs this fits
- **ImagePath** (string?) — Product photo
- **IsActive** (bool) — Soft-deactivate without archive

---

### 7. Scheduling & Calendar

**Current OneManVanFSM `CalendarEvent` model has:**
- Title, StartDateTime, EndDateTime, Duration, Status, Notes, Checklist (JSON)
- FK: JobId, EmployeeId, ServiceAgreementId

**TradeFlow FSM called for but is MISSING:**

| Missing Field | Type | TradeFlow Description | Priority |
|---|---|---|---|
| **EventType** | enum? | Job / Personal / Meeting / Reminder / Block-Off | MEDIUM |
| **IsRecurring** | bool | Recurring event flag | MEDIUM |
| **RecurrenceRule** | string? | iCal RRULE format for repeat patterns | MEDIUM |
| **Color / Tag** | string? | Visual categorization on calendar | LOW |
| **Location** | string? | Address or site name for the event | MEDIUM |
| **ReminderMinutes** | int? | Alert X minutes before | LOW |
| **GoogleCalendarEventId** | string? | For external calendar bi-sync | LOW (future) |

**Missing Relationships:**
- **CalendarEvent ? Customer** — No direct link; currently goes through Job
- **CalendarEvent ? Site** — Same; relies on Job?Site

---

### 8. Time Tracking

**Current OneManVanFSM `TimeEntry` model has:**
- StartTime, EndTime, Hours, OvertimeHours, IsBillable, Notes
- FK: EmployeeId, JobId

**TradeFlow FSM called for but is MISSING:**

| Missing Field | Type | TradeFlow Description | Priority |
|---|---|---|---|
| **TimeCategory** | enum/string? | Travel / On-Site / Diagnostic / Admin / Break — type breakdown | HIGH |
| **GpsLatitude / GpsLongitude** | double? | GPS stamp on clock-in/out | MEDIUM |
| **PhotoOnClockIn** | string? | Photo verification path | LOW |
| **HazardPayFlag** | bool? | Flag for hazardous conditions | LOW |
| **BreakdownCode** | string? | Custom categorization code | LOW |
| **AssetId** | int? (FK) | Log time against specific asset serial — TradeFlow: "2 hours on Serial #XYZ" | HVAC: MEDIUM |
| **ApprovalStatus** | enum? | Pending / Approved / Rejected — for sub/employee timesheet review | MEDIUM |

---

### 9. Invoicing & Payments

**Current models are fairly complete. Notable gaps:**

**Invoice missing:**
| Missing Field | Type | Description | Priority |
|---|---|---|---|
| **InvoiceDate** | DateTime | When invoice was issued (distinct from CreatedAt) | HIGH |
| **DiscountAmount** | decimal? | Applied discount | MEDIUM |
| **DiscountCode** | string? | Promotional or loyalty code | LOW |
| **DepositApplied** | decimal? | Deposit credited | MEDIUM |
| **AfterHoursSurcharge** | decimal? | HVAC-specific add-on | LOW |
| **PaymentTerms** | string? | "Net 30", "Due on Receipt" | MEDIUM |
| **RecurringSchedule** | string? | For recurring invoices (e.g., monthly service agreement billing) | MEDIUM |

**InvoiceLine missing (same as EstimateLine):**
- **LineType** (enum: Labor / Material / Equipment / Fee / Discount)
- **Unit** (string)

**Payment model is solid.** Could add:
- **SquareTransactionId** (string?) — For Square SDK integration tracking (future)

---

### 10. Employee / Workforce

**Current model is fairly robust. Gaps:**

| Missing Field | Type | Description | Priority |
|---|---|---|---|
| **EmergencyContactName** | string? | Safety requirement | MEDIUM |
| **EmergencyContactPhone** | string? | Safety requirement | MEDIUM |
| **LicenseNumber** | string? | Trade license (EPA 608, state HVAC license, etc.) | HVAC: HIGH |
| **LicenseExpiry** | DateTime? | Track renewal | HVAC: HIGH |
| **DriversLicense** | string? | For fleet/insurance | LOW |
| **UniformSize** | string? | Logistics | LOW |
| **VehicleAssigned** | string? | Truck number/plate for routing | MEDIUM |
| **SkillSet / Trades** | string/JSON? | What trades/skills (HVAC, Plumbing, Electrical) | MEDIUM |
| **OvertimeRate** | decimal? | Separate from HourlyRate | MEDIUM |

---

### 11. Service Agreements

**Current model is solid. Minor gaps:**

| Missing Field | Type | Description | Priority |
|---|---|---|---|
| **RenewalDate** | DateTime? | When auto-renewal or reminder triggers | MEDIUM |
| **AutoRenew** | bool | Auto-renew flag | MEDIUM |
| **TradeType** | enum? | What trade the agreement covers | MEDIUM |
| **IncludedServices** | string/JSON? | Detailed list of what's covered (vs. just "AddOns") | MEDIUM |
| **Discount Percent** | decimal? | Service agreement customer discount | LOW |
| **LinkedAssets** | relation | Which specific assets the agreement covers | MEDIUM |

---

### 12. MISSING TABLES / ENTITIES (Not in current app at all)

These are tables/concepts TradeFlow described that have **no equivalent** in the current OneManVanFSM:

| Missing Entity | Description | TradeFlow Reference | Priority |
|---|---|---|---|
| **WarrantyRecord** | Dedicated warranty tracking (start, term, manufacturer, coverage details, registration #) separate from Asset.WarrantyExpiry | Asset Tracking §3: "Warranty engine computes expirations and sends alerts" | HIGH |
| **JobAsset** (join table) | Many-to-many between Job and Asset — which assets were serviced on which job | Time Tracking §7: "auto-log against assets" | HIGH |
| **AssetServiceLog** | Per-asset maintenance log (filter changes, refrigerant charges, cleaning dates) | Asset §3: "Filter Change History table", "integrated logs" | HIGH |
| **Supplier** | Dedicated supplier table (Name, Contact, AccountNumber, Terms) linked to Products/Inventory | Inventory §5: "link to custom Supplier Table" | MEDIUM |
| **CompatibilityRule** | Asset-to-Product compatibility matrix (e.g., "R-410A only for post-2015 units") | Inventory §5: "compatibility matrix" | MEDIUM |
| **Permit** | Permit tracking (type, number, fee, status, inspection dates) | Jobs: "PermitRequired" implies tracking | LOW |
| **VehicleFleet** | Truck/vehicle tracking for routing and inventory location | Scheduling §6: "GPS routing" | LOW |
| **BackupConfig** | Backup rules and schedules for data export | Backups §10: "Add backup rules" | LOW (future) |

---

## STAGE 1 SUMMARY: Top Priority Gaps

### Critical for HVAC Data Collection (implement first):
1. **Asset: Brand, FuelType, UnitConfiguration, BTURating, FilterSize** — Core HVAC data plate info
2. **Asset: WarrantyStartDate + WarrantyTermYears** — Proper warranty calculation
3. **Job: TradeType, JobType** — Categorize work by trade and type
4. **JobAsset join table** — Link jobs to specific assets serviced
5. **AssetServiceLog** — Per-asset maintenance history (filter changes, charges, etc.)
6. **TimeEntry: TimeCategory** — Travel vs. On-Site vs. Diagnostic breakdown
7. **Site: HeatingFuelSource, GasLineLocation** — Field-critical safety/service info

### Important for All Trades:
8. **Product/Inventory: SKU/PartNumber, Brand, Barcode** — Ordering and scanning
9. **Employee: LicenseNumber, LicenseExpiry** — Trade certification tracking
10. **Invoice: InvoiceDate, PaymentTerms** — Basic invoicing fields
11. **EstimateLine/InvoiceLine: LineType, Unit** — Proper line item categorization
12. **Estimate: VersionNumber** — Revision tracking
13. **CalendarEvent: EventType, IsRecurring, RecurrenceRule** — Scheduling improvements

### Nice-to-Have / Future:
14. Customer: ReferralSource, PreferredBrand, Tags
15. Supplier entity
16. CompatibilityRule entity
17. Asset efficiency ratings (AFUE, HSPF, EER)
18. GPS on TimeEntry
19. Square integration IDs

---

> **Next Stage**: Once you review this and confirm direction, we can break these into implementation phases with specific model changes, migration plans, and UI updates needed per feature area.
