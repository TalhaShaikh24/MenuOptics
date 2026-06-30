// Unit Conversion Utility Functions
// Supports industry-standard distributor units:
// BA, BG, BX, CA, CM, CT, DZ, EA, FL, FT, GA, GM, HR, IN, KG, LB, LT, ML, MR, OZ, PA, PC, PR, PT, RL, TB

const ALLOWED_UNITS = new Set([
    'BA', 'BG', 'BX', 'CA', 'CM', 'CT', 'DZ', 'EA',
    'FL', 'FT', 'GA', 'GM', 'HR', 'IN', 'KG', 'LB',
    'LT', 'ML', 'MR', 'OZ', 'PA', 'PC', 'PR', 'PT',
    'RL', 'TB'
]);

// Full label map for display purposes
const UNIT_LABELS = {
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

// Check if a unit is valid
function isValidUnit(unit) {
    return unit && ALLOWED_UNITS.has(unit.toUpperCase());
}

// Get display label for a unit code
function getUnitLabel(unit) {
    if (!unit) return '';
    return UNIT_LABELS[unit.toUpperCase()] || unit;
}

// Normalize unit to uppercase for consistent comparison
function normalizeUnit(unit) {
    return unit ? unit.toUpperCase() : unit;
}

// Convert quantity from one unit to another.
// Since these are opaque distributor unit codes without fixed numeric relationships,
// conversion only applies when fromUnit === toUnit (identity).
// Cross-unit quantities are returned as-is (pass-through).
function convertUnit(quantity, fromUnit, toUnit) {
    const from = normalizeUnit(fromUnit);
    const to = normalizeUnit(toUnit);

    if (from === to) {
        return quantity;
    }

    // No numeric conversion defined between different distributor unit codes
    if (from && to) {
        console.warn(`No numeric conversion defined from ${from} to ${to}. Returning quantity as-is.`);
    }

    return quantity;
}

// Get all allowed unit codes
function getAllUnits() {
    return Array.from(ALLOWED_UNITS).sort();
}

// Format unit display with unitOfMeasure
function formatUnitDisplay(unit, unitOfMeasure) {
    const normalizedUnit = normalizeUnit(unit);
    const normalizedUom = normalizeUnit(unitOfMeasure);
    if (normalizedUom && normalizedUom !== normalizedUnit) {
        return `${normalizedUom} (${normalizedUnit})`;
    }
    return normalizedUnit || unit;
}

// Calculate ingredient cost — price is per-unit of the stored unit.
// If recipe unit === ingredient unit, direct multiplication. Otherwise pass-through.
function calculateIngredientCost(ingredientPrice, ingredientUnit, recipeQuantity, recipeUnit) {
    const convertedQuantity = convertUnit(recipeQuantity, recipeUnit, ingredientUnit);
    return ingredientPrice * convertedQuantity;
}

// Get conversion info — no numeric conversions available between different codes
function getConversionInfo(fromUnit) {
    return {}; // No cross-unit numeric conversions for distributor unit codes
}

// Example usage:
// convertUnit(5, 'KG', 'KG') => 5  (same unit, identity)
// convertUnit(5, 'KG', 'LB') => 5  (different codes, pass-through with warning)
// getAllUnits() => ['BA', 'BG', 'BX', 'CA', ...]
// getUnitLabel('KG') => 'KG - Kilogram'