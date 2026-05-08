Per-kitchen save system install

Replace or add these scripts:

1. SaveData.cs
   New file. If you already have a SaveData.cs or EmployeeSave.cs file, replace it with this one or delete the older duplicate.

2. SaveManager.cs
   Replace your current SaveManager.cs.

3. scoreManager.cs
   Replace your current scoreManager.cs content. Keep only one ScoreManager class in the project.

4. EmployeeMenu.cs
   Replace your current EmployeeMenu.cs content. This is still the EmployeeManager class.

5. Upgrades.cs
   Replace your current Upgrades.cs, or manually copy the new GetSaveState/ApplySaveState overloads if your local file has newer radio-specific code.

6. DishSpawner.cs
   Replace current DishSpawner.cs.

7. ProfitRate.cs
   Replace current ProfitRate.cs.

8. LoanManager.cs
   Replace current LoanManager.cs if you have not already installed the business-ready loan manager.

9. KitchenBusinessProgress.cs
   Replace current KitchenBusinessProgress.cs.

10. KitchenIdentity.cs
   Optional helper. You only need to add it to a scene if SaveManager cannot resolve the kitchen id from LoanManager.

Inspector setup:

- In every kitchen scene, set LoanManager Kitchen Id to that kitchen's id, like kitchen_1 or kitchen_2.
- On Kitchen 1 only, keep Unlock Other Businesses When All Loans Paid checked.
- On all kitchens, leave Prefer Save Manager Loan Index checked unless you specifically want PlayerPrefs to override save.json loan state.
- ScoreManager now has an optional Dishes Per Second Text field. Assign your HUD dishes/sec TMP text there if you have one.
- ProfitRate still controls money per second text.

What saves independently per kitchen:

- total money
- total dishes
- dish count increment
- dish profit multiplier
- employee counts, costs, upgrades, debuffs, and global employee profit multiplier
- soap, glove, sponge, and optional radio ownership state
- selected sink and purchased sink nodes
- loan index

Dish progression resets per kitchen because DishSpawner unlocks dishes based on that kitchen's saved total dishes.
