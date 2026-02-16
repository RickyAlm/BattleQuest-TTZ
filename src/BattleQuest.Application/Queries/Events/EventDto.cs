namespace BattleQuest.Application.Queries.Events;

/// <summary>
/// DTO representando um evento do jogo BattleQuest.
/// Contém todas as informações de um evento processado do log.
/// </summary>
/// <param name="EventId">Identificador único do evento no banco de dados.</param>
/// <param name="OccurredAt">Timestamp UTC de quando o evento ocorreu.</param>
/// <param name="Channel">Canal do evento (ex: GAME, COMBAT, CHAT, SYSTEM).</param>
/// <param name="ActionType">Tipo de ação do evento (ex: PLAYER_JOIN, ITEM_PICKUP, BOSS_DEFEAT).</param>
/// <param name="PlayerId">ID do player principal envolvido no evento (opcional).</param>
/// <param name="VictimPlayerId">ID do player vítima em eventos de combate (opcional).</param>
/// <param name="KillerPlayerId">ID do player que causou morte em eventos PVP (opcional).</param>
/// <param name="QuestId">ID da quest relacionada ao evento (opcional).</param>
/// <param name="Zone">Nome da zona onde o evento ocorreu (opcional).</param>
/// <param name="Item">Nome do item relacionado ao evento (opcional).</param>
/// <param name="Boss">Nome do boss relacionado ao evento (opcional).</param>
/// <param name="Quantity">Quantidade de itens coletados (opcional).</param>
/// <param name="Xp">Experiência ganha no evento (opcional).</param>
/// <param name="Gold">Ouro ganho ou perdido no evento (opcional).</param>
/// <param name="Hp">Pontos de vida relacionados ao evento (opcional).</param>
/// <param name="Damage">Dano causado em eventos de combate (opcional).</param>
/// <param name="Method">Método de morte ou ação (ex: sword, magic) (opcional).</param>
/// <param name="PlayerLevel">Nível do jogador no momento do evento (opcional).</param>
/// <param name="Points">Pontos de score ganhos (opcional).</param>
/// <param name="Reason">Razão da ação (ex: defeated_monster) (opcional).</param>
/// <param name="LocationX">Coordenada X da localização do evento (opcional).</param>
/// <param name="LocationY">Coordenada Y da localização do evento (opcional).</param>
/// <param name="MessageText">Texto de mensagem em eventos de chat ou sistema (opcional).</param>
/// <param name="Raw">Linha original do log, incluída apenas se solicitado (opcional).</param>
public sealed record EventDto(
	int EventId,
	DateTimeOffset OccurredAt,
	string Channel,
	string ActionType,

	string? PlayerId,
	string? VictimPlayerId,
	string? KillerPlayerId,
	string? QuestId,

	string? Zone,
	string? Item,
	string? Boss,

	int? Quantity,
	int? Xp,
	int? Gold,
	int? Hp,
	int? Damage,
	string? Method,
	int? PlayerLevel,
	int? Points,
	string? Reason,
	int? LocationX,
	int? LocationY,
	string? MessageText,

	string? Raw
);
