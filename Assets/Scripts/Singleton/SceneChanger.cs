using UnityEngine;

public class SceneChanger : MonoBehaviour
{
    public void CambiarEscena(string escena)
    {
        SCManager.instance.LoadScene(escena);
    }
    public void CerrarJuego()
    {
        SCManager.instance.CloseGame();
    }
}
