# OneManVanFSM — Future Features Roadmap

> **Status**: Planned (Near-Future Implementation)
> **Created**: June 2025

---

## 1. Asset QR Code System

**Priority**: High
**Scope**: Assets module + Mobile app

### Description
Generate printable QR code labels for each asset. Technicians scan the QR code on-site to instantly pull up the asset's full history, manuals, specs, and repair timeline without searching.

### Key Requirements
- Generate unique QR codes per asset (encode asset ID / URL)
- Printable label sheet generation (PDF with multiple labels per page)
- Mobile camera integration: scan QR → navigate to asset detail
- Quick actions from scan: log repair, view history, check warranty
- Offline-capable: cache scanned asset data for field use
- Bulk QR generation for all assets at a site/customer

### Integration Points
- Assets detail page: "Generate QR Code" button
- Assets list: bulk QR export
- Mobile app: camera scanner on home/assets page
- Print templates: include QR code on asset reports

---

## 2. Supplier Integration

**Priority**: Medium
**Scope**: Inventory, Products, Purchasing

### Description
Connect with HVAC/trade suppliers for real-time pricing, direct ordering from material lists, and automated warranty registration. Streamlines procurement and reduces manual data entry.

### Key Requirements
- Supplier directory: store supplier contact info, account numbers, pricing tiers
- Real-time price lookup from supplier APIs (Johnstone Supply, Ferguson, etc.)
- Direct purchase order generation from material lists / low-stock alerts
- Price comparison across multiple suppliers for the same product
- Order tracking and delivery status integration
- Receipt/invoice import from supplier portals
- Lot/batch number tracking from supplier to inventory to asset

### Integration Points
- Inventory: auto-reorder when stock hits reorder point
- Material Lists: "Check Supplier Prices" per item
- Products: link to supplier catalog entries
- Expenses: auto-create expense records from supplier invoices
- Assets: trace lot numbers back to supplier purchase

---

## 3. Manufacturer Warranty Portals

**Priority**: Medium
**Scope**: Assets, Warranty/Repairs, Service Agreements

### Description
Integrate with manufacturer warranty systems to auto-verify warranty status by serial number, streamline claim submissions with asset history, and track warranty expiration proactively.

### Key Requirements
- Auto-lookup warranty status by serial number (manufacturer API or manual entry)
- Warranty registration automation on asset creation (submit serial + install date)
- Claim submission workflow: pre-populate from asset repair history
- Warranty coverage details: what's covered, labor allowance, parts allowance
- Expiration alerts: notify office and customer before warranty ends
- Claim status tracking: pending, approved, denied, paid
- Historical claim records linked to assets

### Integration Points
- Assets: warranty status badge, "Check Warranty" button
- Jobs/Repairs: "File Warranty Claim" action from repair completion
- Service Agreements: auto-extend or renew based on manufacturer warranty
- Invoices: flag warranty-covered repairs (no charge to customer)
- Reports: warranty claim success rate, value recovered

---

## 4. Referral Program

**Priority**: Low-Medium
**Scope**: Customers, Marketing, Financials

### Description
Customer referral tracking with automated rewards. Customers who refer new business receive discounts, credits, or other incentives. Tracks referral sources for marketing analytics.

### Key Requirements
- Referral code generation per customer (unique shareable codes)
- Referral tracking: who referred whom, when, conversion status
- Reward tiers: configurable (e.g., $50 credit per referral, 10% off next service)
- Automated reward application: credit to invoice or generate discount code
- Referral dashboard: top referrers, conversion rates, total value generated
- Customer notification: email/SMS when referral converts, reward earned
- Referral source on customer/estimate/job records

### Integration Points
- Customers: "Referral Code" field, "Referred By" field
- Estimates/Jobs: "Referral Source" tracking
- Invoices: apply referral credits/discounts
- Reports: referral program ROI, top referrers
- Settings: configure reward structure, enable/disable program

---

## 5. Plumbing / Electrical Trade Expansions

**Priority**: Medium
**Scope**: App-wide (Products, Assets, Jobs, Estimates, Settings)

### Description
Expand trade-specific functionality beyond HVAC to fully support plumbing and electrical contractors. Add trade-specific fields, templates, compliance tracking, and terminology.

### Plumbing Requirements
- Asset fields: capacity/gallons, flow rate (GPM), pipe material, fixture type
- Compliance: backflow prevention testing, cross-connection control
- Product templates: water heaters, fixtures, pipe fittings, valves
- Job types: drain cleaning, re-pipe, fixture install, water heater replacement
- Material list templates: common plumbing kits
- Code reference: plumbing code lookup by jurisdiction

### Electrical Requirements
- Asset fields: amp rating, circuits, panel type, voltage, phase
- Compliance: GFCI/AFCI protection tracking, panel labeling
- Product templates: panels, breakers, outlets, switches, wire
- Job types: panel upgrade, circuit install, troubleshooting, EV charger
- Material list templates: common electrical kits
- Code reference: NEC code lookup, permit requirements
- Load calculations: service sizing, circuit loading

### General Requirements
- Trade selector in Settings: configure default trade, available trades
- Trade-specific field visibility: show/hide fields based on selected trade
- Template library per trade: pre-built material lists, job checklists
- Customizable terminology: "Furnace" → "Water Heater" → "Panel"
- Multi-trade support: single company can handle multiple trades

### Integration Points
- Settings > Customizations: trade-specific dropdown options
- Products: trade category filtering
- Assets: trade-specific fields shown/hidden per asset type
- Estimates/Jobs: trade type drives available templates and fields
- Reports: per-trade revenue, job volume, profitability

---

## Implementation Notes

### Suggested Order
1. **Asset QR Code System** — Quick win, high field efficiency impact
2. **Plumbing/Electrical Expansions** — Widens market, leverages existing customization framework
3. **Supplier Integration** — Significant procurement efficiency, requires API partnerships
4. **Manufacturer Warranty Portals** — Depends on manufacturer API availability
5. **Referral Program** — Marketing feature, lower priority for core operations

### Technical Considerations
- QR codes: use SkiaSharp or QRCoder library for generation
- Supplier APIs: abstract behind interface for multiple supplier adapters
- Warranty portals: start with manual entry, add API integrations incrementally
- Trade expansions: leverage existing DropdownService customization framework
- Referral program: new database tables (ReferralCode, Referral, ReferralReward)
