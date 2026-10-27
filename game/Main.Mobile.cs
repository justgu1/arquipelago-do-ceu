using Godot;

namespace ArquipelagoDoCeu;

// O que só o celular pede ao jogo principal: o botão Voltar do Android e a pausa em segundo plano.
public partial class Main
{
	void OnMobileNotification(int what)
	{
		switch ((long)what)
		{
			// O Voltar faz o mesmo que o Esc: fecha a tela ou abre o Menu da Dungeon. Nunca fecha o jogo.
			case NotificationWMGoBackRequest:
				Input.ParseInputEvent(new InputEventAction { Action = "ui_cancel", Pressed = true });
				Input.ParseInputEvent(new InputEventAction { Action = "ui_cancel", Pressed = false });
				break;
			// Em segundo plano o jogo congela: a Energia não pode cair nem bolha nenhuma chegar com a criança longe.
			case NotificationApplicationFocusOut when DeviceInfo.IsTouch:
				GetTree().Paused = true;
				break;
			case NotificationApplicationFocusIn when DeviceInfo.IsTouch:
				GetTree().Paused = false;
				break;
		}
	}
}
