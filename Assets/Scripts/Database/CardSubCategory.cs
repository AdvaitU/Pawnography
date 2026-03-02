/*
 * ============================================================
 * SCRIPT:      CardSubCategory.cs
 * GAMEOBJECT:  Not present on any GameObject.
 * ------------------------------------------------------------
 * FUNCTION:
 *   Defines the enum used to demarcate sub-categories of 
 *   item/seller cards.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *  CardData             -- To set SubCategory for item/seller 
 *                          cards
 *  CardInteractionManager -- To check SubCategory for card 
 *                            effects
 *  ShopManager         -- To check SubCategory for auto-
 *                         identification
 *  InventoryManager    -- To check SubCategory for auto-
 *                         identification
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:   None (data container only)
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   No runtime methods. Pure data asset.
 * ============================================================
 */

public enum CardSubCategory
{
    None,               // Default / unassigned
    AncientArtefact, // Items with significant historical importance
    ModernArtefact,    // Items from the modern era with cultural significance
    Collectible,        // Items sought after by collectors
    MusicalInstrument,  // Musical Instrument of rare value
    Artwork,            // Paintings, sculptures, and other art pieces
    Technology,         // Old or rare tech items
    FloraAndFauna,      // Rare plants and animals (e.g., exotic pets, rare flowers)
    SportsMemorabilia,  // Items related to sports history (e.g., signed jerseys, vintage equipment)
}