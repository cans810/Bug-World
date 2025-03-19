// This class should be removed entirely as its functionality is already in LivingEntity
// To migrate any existing PlayerHitbox instances to use LivingEntity:
// 1. Add EntityHitbox component instead
// 2. Configure the EntityHitbox's owner and targetLayers
// 3. Configure attack settings on the owner's LivingEntity component