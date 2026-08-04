/* ============================================================================
 * unitConversions.js — Shared unit codes and conversion helpers.
 * Exposes: NEW_UNIT_LABELS, ALLOWED_UNIT_OF_MEASURE_LABELS, convertUnit,
 *          getAllowedUomForUnit, buildUomOptionsHtml, normalizeUnit.
 * Load this BEFORE any page script that prices ingredients (recipes, menu,
 * CogsReport). Do not change conversion factors.
 * ==========================================================================*/

// ============================================================================
// NEW_UNIT_LABELS
// ----------------------------------------------------------------------------
// Full set of unit codes the system recognises. Used to populate the
// ingredient's BASE "Unit" dropdown (an ingredient can be stocked in any of
// these). Unchanged from the original list.
// ============================================================================
const NEW_UNIT_LABELS = {
    'BA': 'BA - Bag',
    'BG': 'BG - Bag (alt)',
    'BX': 'BX - Box',
    'CA': 'CA - Case',
    'CM': 'CM - Centimeter',
    'CT': 'CT - Carton',
    'DZ': 'DZ - Dozen',
    'EA': 'EA - Each',
    'FL': 'FL - Fluid Ounce',
    'FT': 'FT - Foot',
    'GA': 'GA - Gallon',
    'GM': 'GM - Gram',
    'HR': 'HR - Hour',
    'IN': 'IN - Inch',
    'KG': 'KG - Kilogram',
    'LB': 'LB - Pound',
    'LT': 'LT - Liter',
    'ML': 'ML - Milliliter',
    'MR': 'MR - Meter',
    'OZ': 'OZ - Ounce',
    'PA': 'PA - Pail',
    'PC': 'PC - Piece',
    'PR': 'PR - Pair',
    'PT': 'PT - Pint',
    'RL': 'RL - Roll',
    'TB': 'TB - Tube'
};

// ============================================================================
// ALLOWED_UNIT_OF_MEASURE_LABELS
// ----------------------------------------------------------------------------
// The ONLY units that may appear in the per-ingredient-row "Unit of Measure"
// dropdown (ingredient-detail-row). This is a DIFFERENT, smaller set than
// NEW_UNIT_LABELS. The row dropdown shows the units from this set that are
// convertible with the ingredient's base unit (see getAllowedUomForUnit).
// ============================================================================
const ALLOWED_UNIT_OF_MEASURE_LABELS = {
    'EA': 'EA - Each',
    'FL': 'FL - Fluid Ounce',
    'GA': 'GA - Gallon',
    'GM': 'GM - Gram',
    'IN': 'IN - Inch',
    'KG': 'KG - Kilogram',
    'LB': 'LB - Pound',
    'LT': 'LT - Liter',
    'ML': 'ML - Milliliter',
    'OZ': 'OZ - Ounce',
    'PC': 'PC - Piece'
};

// Unit Conversion Utility Functions
// Supports every code in NEW_UNIT_LABELS so any base unit can be converted.
//
// NOTE ON WEIGHT <-> VOLUME:
// In this menu/recipe costing system ingredients are frequently entered by
// weight (KG/GM/LB/OZ) but used in recipes by volume (LT/ML/FL/GA/PT) and
// vice versa. To make the UOM dropdown usable, weight and volume are treated
// as a single convertible group using a density of 1 (i.e. 1 gram == 1
// millilitre, the density of water). This is the standard simplifying
// assumption used for culinary costing. If you need product-specific density,
// apply it separately from this table.
//
// NOTE ON PACKAGING UNITS (BA, BG, BX, CA, CT, PA, RL, TB):
// These have no fixed numeric conversion ratio (the quantity inside a
// Box/Case/etc. varies per product), so each is treated as its own standalone
// category and only "converts" to itself.

const CONVERSION_RATES = {
    // Weight + Volume merged into ONE group so e.g. KG <-> LT works.
    // Base unit: grams (with 1 ml assumed == 1 g).
    mass_volume: {
        // Weight
        'GM': 1,
        'KG': 1000,
        'LB': 453.59237,
        'OZ': 28.349523125,
        // Volume (1 ml == 1 g assumption)
        'ML': 1,
        'LT': 1000,
        'FL': 29.5735295625,   // fluid ounce
        'GA': 3785.411784,     // gallon
        'PT': 473.176473       // pint
    },
    // Length conversions (base unit: centimeters)
    length: {
        'CM': 1,
        'MR': 100,
        'IN': 2.54,
        'FT': 30.48
    },
    // Count-based conversions (base unit: each)
    count: {
        'EA': 1,
        'PC': 1,
        'DZ': 12,
        'PR': 2
    },
    // Time (single unit, kept for completeness / future use)
    time: {
        'HR': 1
    }
};

// Packaging units that have no fixed conversion ratio to anything else.
// Each is mapped to its own unique category name so it only matches itself.
const PACKAGING_UNITS = ['BA', 'BG', 'BX', 'CA', 'CT', 'PA', 'RL', 'TB'];

// Normalize a unit value into its canonical uppercase code.
// Tolerates lowercase/mixed case ("kg" -> "KG"), surrounding whitespace, and
// full label strings ("KG - Kilogram" -> "KG").
function normalizeUnit(unit) {
    if (unit === null || unit === undefined) return unit;
    const upper = String(unit).trim().toUpperCase();
    const match = upper.match(/^[A-Z]+/); // leading letters are the code
    return match ? match[0] : upper;
}

// Determine the category of a unit (case-insensitive)
function getUnitCategory(unit) {
    const u = normalizeUnit(unit);
    if (Object.keys(CONVERSION_RATES.mass_volume).includes(u)) return 'mass_volume';
    if (Object.keys(CONVERSION_RATES.length).includes(u)) return 'length';
    if (Object.keys(CONVERSION_RATES.count).includes(u)) return 'count';
    if (Object.keys(CONVERSION_RATES.time).includes(u)) return 'time';
    if (PACKAGING_UNITS.includes(u)) return `packaging_${u}`; // standalone category per packaging unit
    return null;
}

// Convert quantity from one unit to another
function convertUnit(quantity, fromUnit, toUnit, size) {
    // Normalize so lowercase/label inputs still convert correctly.
    fromUnit = normalizeUnit(fromUnit);
    toUnit = normalizeUnit(toUnit);

    // If units are the same, no conversion needed
    if (fromUnit === toUnit) {
        return quantity;
    }

    const fromCategory = getUnitCategory(fromUnit);
    const toCategory = getUnitCategory(toUnit);

    const itemSize = (size !== undefined && size !== null && !isNaN(parseFloat(size)) && parseFloat(size) > 0)
        ? parseFloat(size)
        : 1;

    // Cross-category conversion between count (EA/PC/etc.) and mass_volume
    if (fromCategory === 'count' && toCategory === 'mass_volume') {
        // e.g., 2 EA where 1 EA = 10 GM -> quantity (2) * size (10) = 20 GM
        const qtyInBaseMassVolume = quantity * itemSize;
        const toRate = (CONVERSION_RATES.mass_volume && CONVERSION_RATES.mass_volume[toUnit]) || 1;
        return qtyInBaseMassVolume / toRate;
    }

    if (fromCategory === 'mass_volume' && toCategory === 'count') {
        // e.g., 20 GM to EA where 1 EA = 10 GM -> quantity (20) / size (10) = 2 EA
        const fromRate = (CONVERSION_RATES.mass_volume && CONVERSION_RATES.mass_volume[fromUnit]) || 1;
        const qtyInBaseMassVolume = quantity * fromRate;
        const qtyInEa = qtyInBaseMassVolume / itemSize;
        const toRate = (CONVERSION_RATES.count && CONVERSION_RATES.count[toUnit]) || 1;
        return qtyInEa / toRate;
    }

    // Can't convert between different categories (e.g., mass/volume to length,
    // or between two different packaging units like BX -> CA)
    if (fromCategory !== toCategory || !fromCategory) {
        console.warn(`Cannot convert from ${fromUnit} to ${toUnit} - different categories (or packaging unit requiring packSize)`);
        return quantity;
    }

    // Packaging units only match themselves (handled by fromUnit === toUnit above),
    // so if we reach here with a packaging category, there's nothing to convert.
    if (fromCategory.startsWith('packaging_')) {
        return quantity;
    }

    // Get conversion rates
    const conversionTable = CONVERSION_RATES[fromCategory];

    // Convert to base unit first, then to target unit
    const baseQuantity = quantity * conversionTable[fromUnit];
    const convertedQuantity = baseQuantity / conversionTable[toUnit];

    return convertedQuantity;
}

// Get ALL units convertible with the given unit (full category, unfiltered).
function getCompatibleUnits(unit) {
    const u = normalizeUnit(unit);
    const category = getUnitCategory(u);
    if (!category) return [u];
    if (category.startsWith('packaging_')) return [u]; // only itself
    return Object.keys(CONVERSION_RATES[category]);
}

// Get the units that should appear in the ingredient-detail-row UOM dropdown
// for a given base unit: the ALLOWED units that are convertible with it.
// Falls back to the base unit itself so the dropdown is never empty.
function getAllowedUomForUnit(baseUnit) {
    const base = normalizeUnit(baseUnit);
    if (!base) return [];
    const allowedCodes = Object.keys(ALLOWED_UNIT_OF_MEASURE_LABELS);
    const compatible = getCompatibleUnits(base);
    const allowedCompatible = compatible.filter(u => allowedCodes.includes(u));

    const category = getUnitCategory(base);
    if (category === 'mass_volume') {
        if (allowedCodes.includes('EA') && !allowedCompatible.includes('EA')) {
            allowedCompatible.push('EA');
        }
    }

    return allowedCompatible.length > 0 ? allowedCompatible : [base];
}

// Build <option> HTML for an ingredient-detail-row "Unit of Measure" dropdown.
// Shared by menu.js and recipes.js. Restricted to ALLOWED_UNIT_OF_MEASURE_LABELS
// AND to units convertible with the ingredient's base unit. Case-insensitive.
function buildUomOptionsHtml(baseUnit, selectedUnit) {
    const placeholder = `<option value="" disabled${selectedUnit ? '' : ' selected'}>Select unit of measure</option>`;

    if (!baseUnit) {
        return placeholder;
    }

    const compatibleUnits = (typeof getAllowedUomForUnit === 'function')
        ? getAllowedUomForUnit(baseUnit)
        : [baseUnit];

    const normalize = (typeof normalizeUnit === 'function') ? normalizeUnit : (u => u);
    const currentSelection = normalize(selectedUnit || baseUnit);

    let optionsHtml = placeholder;
    compatibleUnits.forEach(function (unitCode) {
        const label = (typeof ALLOWED_UNIT_OF_MEASURE_LABELS !== 'undefined' && ALLOWED_UNIT_OF_MEASURE_LABELS[unitCode])
            ? ALLOWED_UNIT_OF_MEASURE_LABELS[unitCode]
            : ((typeof NEW_UNIT_LABELS !== 'undefined' && NEW_UNIT_LABELS[unitCode]) ? NEW_UNIT_LABELS[unitCode] : unitCode);
        const isSelected = normalize(unitCode) === currentSelection ? ' selected' : '';
        optionsHtml += `<option value="${unitCode}"${isSelected}>${label}</option>`;
    });

    return optionsHtml;
}

// Format unit display with unitOfMeasure
function formatUnitDisplay(unit, unitOfMeasure) {
    if (unitOfMeasure && unitOfMeasure !== unit) {
        return `${unitOfMeasure} (${unit})`;
    }
    return unit;
}

// Calculate ingredient cost with unit conversion
function calculateIngredientCost(ingredientPrice, ingredientUnit, recipeQuantity, recipeUnit, size) {
    // Convert recipe quantity to ingredient unit
    const convertedQuantity = convertUnit(recipeQuantity, recipeUnit, ingredientUnit, size);
    return ingredientPrice * convertedQuantity;
}

// Get unit conversion info for display
function getConversionInfo(fromUnit) {
    const category = getUnitCategory(fromUnit);
    if (!category) return {};
    const conversions = {};
    const compatibleUnits = getCompatibleUnits(fromUnit);
    compatibleUnits.forEach(toUnit => {
        if (toUnit !== fromUnit) {
            const converted = convertUnit(1, fromUnit, toUnit);
            conversions[toUnit] = converted;
        }
    });
    return conversions;
}

// Get the display label for a unit.
// Prefers the ALLOWED label, falls back to the full NEW_UNIT_LABELS set.
function getUnitLabel(unit) {
    const u = normalizeUnit(unit);
    return ALLOWED_UNIT_OF_MEASURE_LABELS[u] || NEW_UNIT_LABELS[u] || u;
}

// Example usage:
// convertUnit(1, 'KG', 'GM')       => 1000
// convertUnit(1, 'KG', 'LB')       => 2.2046226218...
// convertUnit(1, 'KG', 'LT')       => 1        (1 kg == 1 L at density 1)
// convertUnit(1, 'KG', 'ML')       => 1000
// convertUnit(1, 'LT', 'ML')       => 1000
// getCompatibleUnits('KG')         => ['GM','KG','LB','OZ','ML','LT','FL','GA','PT']  (full category)
// getAllowedUomForUnit('KG')       => ['GM','KG','LB','OZ','ML','LT','FL','GA']        (row dropdown, PT excluded)
// getAllowedUomForUnit('EA')       => ['EA','PC']
// getAllowedUomForUnit('BX')       => ['BX']  (packaging: only itself)
