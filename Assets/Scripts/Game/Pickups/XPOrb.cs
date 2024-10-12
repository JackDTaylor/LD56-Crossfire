using UnityEngine;

public class XPOrb : MonoBehaviour
{
    public int xpAmount = 1;

    public float magnetSpeed = 3.5f;
    public float magnetDistance = 3.5f;
    public float approximateDistance = 5f;
    public float pickupDistance = 1f;
    public float consumeDistance = 0.1f;

    public Rigidbody2D rigidbody2d;

    SpriteRenderer spriteRenderer;

    bool isPickingUp;
    AntController pickupTarget;

    float untouchableCooldown = 1f;

    void Start() {
        spriteRenderer = GetComponent<SpriteRenderer>();

        rigidbody2d.AddForce(Random.insideUnitCircle * 35f);
    }

    void UpdateColor() {
        spriteRenderer.color = xpAmount switch {
            > 25 => Color.red,
            > 10 => new Color(1f, 0.5f, 0, 1f),
            > 5 => Color.yellow,
            _ => Color.green,
        };
    }

    void Update() {
        UpdateColor();

        if(untouchableCooldown > 0) {
            untouchableCooldown -= Time.deltaTime;
            return;
        }

        if(PlayerStatsManager.Get().player == null) {
            if(isPickingUp) {
                Consume();
            }

            return;
        }

        float playerScale =  PlayerStatsManager.Get().player.CalcScale();

        Vector2 playerPosition = PlayerStatsManager.Get().player.transform.position;
        Vector2 myPosition = transform.position;

        if(Mathf.Abs(playerPosition.x - myPosition.x) > approximateDistance * playerScale || Mathf.Abs(playerPosition.y - myPosition.y) > approximateDistance * playerScale) {
            return;
        }

        var distance = (playerPosition - myPosition).magnitude;

        if(distance < consumeDistance) {
            Consume();
            return;
        }

        if(!isPickingUp && distance < pickupDistance) {
            Pickup();
        }

        if(distance < magnetDistance * playerScale) {
            var currentMagnetSpeed = (100 / distance) * magnetSpeed;

            // Accelerate towards player
            Vector2 newPosition = Vector2.Lerp(transform.position, playerPosition, currentMagnetSpeed * Time.deltaTime);

            rigidbody2d.MovePosition(newPosition);
        }
    }

    void Pickup() {
        isPickingUp = true;
        GetComponent<Collider2D>().enabled = false;
    }

    void Consume() {
        PlayerStatsManager.Get().AddXp(xpAmount);

        transform.SetParent(null); // Become batman
        Destroy(gameObject);
    }
}