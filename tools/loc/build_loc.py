"""Fuente única de textos (es, en, pt). Genera Assets/_Tesela/Config/Resources/Loc/{es,en,pt}.json.
Uso: python3 tools/loc/build_loc.py"""
import json, os
T = {}
def k(key, es, en, pt): T[key] = (es, en, pt)

k("app.name","Tesela","Tesela","Tesela")
k("app.tagline","El block puzzle justo: sin trampas, sin anuncios en tu cara, y cada línea restaura algo bello.","The fair block puzzle: no tricks, no ads in your face, and every line restores something beautiful.","O block puzzle justo: sem truques, sem anúncios na sua cara, e cada linha restaura algo bonito.")
# Modos
k("mode.classic","Clásico","Classic","Clássico")
k("mode.restoration","Restauración","Restoration","Restauração")
k("mode.daily","Reto diario","Daily challenge","Desafio diário")
k("mode.daily.played","Reto diario · {0}","Daily · {0}","Desafio · {0}")
k("mode.zen","Zen","Zen","Zen")
k("mode.zen.subtitle","Sin récord, sin prisa","No score chase, no rush","Sem recorde, sem pressa")
k("mode.event","Evento","Event","Evento")
k("event.title","Evento de la semana","Event of the week","Evento da semana")
k("event.free_rotation","Giro libre","Free spin","Giro livre")
k("event.regions","Patio grande 9×9","Big patio 9×9","Pátio grande 9×9")
k("event.bombs","Azulejos con mecha","Fused tiles","Azulejos com pavio")
k("event.golden","Azulejos dorados","Golden tiles","Azulejos dourados")
k("home.continue","Continuar partida · {0}","Continue game · {0}","Continuar partida · {0}")
# Navegación
k("nav.shop","Tienda","Shop","Loja"); k("nav.pass","Pase","Pass","Passe"); k("nav.album","Álbum","Album","Álbum")
k("nav.achievements","Logros","Achievements","Conquistas"); k("nav.settings","Ajustes","Settings","Ajustes")
# Juego
k("game.best","Mejor: {0}","Best: {0}","Melhor: {0}")
k("game.streak","Racha ×{0}","Streak ×{0}","Sequência ×{0}")
k("game.moves_left","{0} jugadas","{0} moves","{0} jogadas")
k("game.coins_short","{0}","{0}","{0}")
k("game.lines2","¡Doble junta!","Double joint!","Junta dupla!")
k("game.lines3","¡Junta perfecta!","Perfect joint!","Junta perfeita!")
k("game.lines4","¡Mosaico completo!","Full mosaic!","Mosaico completo!")
k("game.board_clean","¡Patio limpio!","Clean patio!","Pátio limpo!")
# Potenciadores
for key,es,en,pt,des,den,dpt in [
 ("undo","Deshacer","Undo","Desfazer","Vuelve atrás tu última jugada.","Take back your last move.","Volta a sua última jogada."),
 ("rotate","Girar","Rotate","Girar","Gira una pieza de la bandeja 90°.","Turn a tray piece 90°.","Gira uma peça da bandeja 90°."),
 ("hammer","Martillo","Hammer","Martelo","Quita un azulejo del patio.","Remove one tile from the patio.","Remove um azulejo do pátio."),
 ("broom","Escoba","Broom","Vassoura","Barre una fila o columna.","Sweep a row or column.","Varre uma linha ou coluna."),
 ("newtray","Nueva bandeja","New tray","Nova bandeja","Cambia las piezas de la bandeja.","Swap the pieces on the tray.","Troca as peças da bandeja."),
 ("reserve","Reserva","Hold","Reserva","Abre una casilla para guardar una pieza durante la partida.","Opens a slot to hold one piece for the whole game.","Abre um espaço para guardar uma peça durante a partida.")]:
    k(f"booster.{key}.name",es,en,pt); k(f"booster.{key}.desc",des,den,dpt)
k("booster.hammer.hint","Toca el azulejo que quieres quitar","Tap the tile to remove","Toque o azulejo que quer remover")
k("booster.broom.hint_row","Toca una fila · cambiar a columna","Tap a row · switch to column","Toque uma linha · mudar para coluna")
k("booster.broom.hint_col","Toca una columna · cambiar a fila","Tap a column · switch to row","Toque uma coluna · mudar para linha")
k("booster.rotate.hint","Toca la pieza que quieres girar","Tap the piece to rotate","Toque a peça que quer girar")
k("booster.reserve.hint","Toca una pieza para guardarla o cambiarla","Tap a piece to hold or swap it","Toque uma peça para guardá-la ou trocá-la")
k("booster.free_ad","Uno gratis con un video","One free with a video","Um grátis com um vídeo")
k("booster.go_shop","Ir a la tienda","Go to shop","Ir à loja")
# Pausa / revivir
k("pause.title","Pausa","Paused","Pausa"); k("pause.resume","Seguir jugando","Keep playing","Continuar jogando")
k("pause.restart","Empezar de nuevo","Start over","Recomeçar"); k("pause.settings","Ajustes","Settings","Ajustes")
k("pause.home","Ir a Inicio","Go home","Ir ao início"); k("pause.saved","Tu partida se guarda tras cada jugada.","Your game is saved after every move.","Sua partida é salva após cada jogada.")
k("revive.title","Sin espacio","Out of room","Sem espaço")
k("revive.body","Puedes seguir una vez: se barren las dos filas más llenas y llega una bandeja nueva.","You can continue once: the two fullest rows are swept and a new tray arrives.","Você pode continuar uma vez: as duas linhas mais cheias são varridas e chega uma bandeja nova.")
k("revive.ad","Seguir con un video","Continue with a video","Continuar com um vídeo")
k("revive.coins","Seguir por {0} monedas","Continue for {0} coins","Continuar por {0} moedas")
k("revive.decline","Terminar partida","End game","Encerrar partida")
k("ads.title","Video","Video","Vídeo")
k("ads.unavailable","Ahora mismo no hay video disponible. Puedes usar monedas o intentarlo más tarde.","No video is available right now. You can use coins or try later.","Agora não há vídeo disponível. Você pode usar moedas ou tentar mais tarde.")
k("ads.unavailable_short","No hay video disponible ahora.","No video available right now.","Nenhum vídeo disponível agora.")
# Resultados
k("results.title","Partida terminada","Game over","Fim de jogo"); k("results.zen","Buen rato","Nice session","Bom momento")
k("results.level_won","Mosaico restaurado","Mosaic restored","Mosaico restaurado"); k("results.level_lost","Casi lo logras","So close","Quase conseguiu")
k("results.new_best","¡Nuevo récord!","New best!","Novo recorde!")
k("results.seed","Semilla {0}","Seed {0}","Semente {0}")
k("results.level","Nivel {0}","Level {0}","Nível {0}")
k("results.level_up","Subiste al nivel {0}","You reached level {0}","Você chegou ao nível {0}")
k("results.missions_ready","{0} misión(es) lista(s) para cobrar","{0} mission(s) ready to claim","{0} missão(ões) pronta(s) para resgatar")
k("results.streak_chest","Cofre de racha de 7 días abierto","7-day streak chest opened","Baú de sequência de 7 dias aberto")
k("results.tileset","Completaste un set de azulejos","You completed a tile set","Você completou um conjunto de azulejos")
k("results.achievement","Logro desbloqueado","Achievement unlocked","Conquista desbloqueada")
k("results.double_coins","Duplicar monedas con un video","Double coins with a video","Dobrar moedas com um vídeo")
k("results.again","Otra partida","Play again","Jogar de novo"); k("results.next_level","Siguiente nivel","Next level","Próximo nível")
k("results.retry","Intentar de nuevo","Try again","Tentar de novo"); k("results.home","Inicio","Home","Início")
# Misiones
k("missions.daily","Misiones de hoy","Today's missions","Missões de hoje")
for m,es,en,pt in [("games","Juega {0} partidas","Play {0} games","Jogue {0} partidas"),("lines","Limpia {0} líneas","Clear {0} lines","Limpe {0} linhas"),
 ("pieces","Coloca {0} piezas","Place {0} pieces","Coloque {0} peças"),("boosters","Usa {0} potenciador(es)","Use {0} booster(s)","Use {0} poder(es)"),
 ("score_run","Haz {0} puntos en una partida","Score {0} in one game","Faça {0} pontos numa partida"),("streak_run","Logra una racha de {0}","Reach a streak of {0}","Alcance uma sequência de {0}"),
 ("daily","Juega el reto diario {0} vez/veces","Play the daily challenge {0} time(s)","Jogue o desafio diário {0} vez(es)"),("levels","Completa {0} nivel(es)","Complete {0} level(s)","Complete {0} nível(is)"),
 ("stars","Gana {0} estrellas","Earn {0} stars","Ganhe {0} estrelas"),("empty_board","Deja el patio limpio {0} vez/veces","Clear the whole patio {0} time(s)","Limpe o pátio inteiro {0} vez(es)")]:
    k("mission."+m,es,en,pt)
# Capítulos
k("chapter.1","Capítulo 1 · La plaza","Chapter 1 · The square","Capítulo 1 · A praça")
k("chapter.2","Capítulo 2 · La fuente","Chapter 2 · The fountain","Capítulo 2 · A fonte")
k("chapter.3","Capítulo 3 · La capilla","Chapter 3 · The chapel","Capítulo 3 · A capela")
k("album.progress","{0}/{1} estrellas","{0}/{1} stars","{0}/{1} estrelas")
k("album.tilesets","Sets completos: {0} · siguiente: {1}/{2} azulejos","Sets completed: {0} · next: {1}/{2} tiles","Conjuntos completos: {0} · próximo: {1}/{2} azulejos")
# Logros
for a,es,en,pt in [("first_steps","Primeros pasos","First steps","Primeiros passos"),("line_artisan","Artesano de líneas","Line artisan","Artesão de linhas"),
 ("clean_slate","Patio impecable","Spotless patio","Pátio impecável"),("streak_master","Maestro de rachas","Streak master","Mestre das sequências"),
 ("restorer","Restaurador","Restorer","Restaurador"),("classic_score","Puntaje de taller","Workshop score","Pontuação de oficina"),
 ("collector","Coleccionista","Collector","Colecionador"),("loyal","Visita diaria","Daily visit","Visita diária")]:
    k("ach."+a,es,en,pt)
k("ach.progress","{0} / {1} · escalón {2} de {3}","{0} / {1} · tier {2} of {3}","{0} / {1} · nível {2} de {3}")
k("ach.leaderboards","Clasificaciones","Leaderboards","Classificações")
# Tienda
k("shop.balance","Tienes {0} monedas","You have {0} coins","Você tem {0} moedas")
k("shop.buy_for","Comprar por {0}","Buy for {0}","Comprar por {0}")
k("shop.no_ads","Sin anuncios","No ads","Sem anúncios")
k("shop.no_ads.desc","Quita intersticiales y banners para siempre. Incluye 3 protectores de racha. Los videos con premio siguen siendo opcionales.","Removes interstitials and banners forever. Includes 3 streak shields. Reward videos stay optional.","Remove intersticiais e banners para sempre. Inclui 3 protetores de sequência. Vídeos com prêmio continuam opcionais.")
k("shop.starter","Pack inicial","Starter pack","Pacote inicial")
k("shop.starter.desc","500 monedas, 5 potenciadores y el tema Cobalto antiguo. Oferta única de 48 h.","500 coins, 5 boosters and the Old Cobalt theme. One-time 48 h offer.","500 moedas, 5 poderes e o tema Cobalto antigo. Oferta única de 48 h.")
k("shop.piggy","Hucha de azulejos","Tile piggy bank","Cofrinho de azulejos")
k("shop.piggy_value","Se llena mientras juegas. Ahora contiene {0} monedas.","Fills up as you play. It now holds {0} coins.","Enche enquanto você joga. Agora tem {0} moedas.")
k("shop.club","Club Tesela","Tesela Club","Clube Tesela")
k("shop.club_terms","{0} al mes, se renueva cada mes. Cancela cuando quieras en Google Play > Suscripciones. Sin anuncios, 100 monedas diarias, 1 potenciador diario de cada tipo, temas del Club y ×1,5 XP del pase.","{0} per month, renews monthly. Cancel anytime in Google Play > Subscriptions. No ads, 100 daily coins, 1 daily booster of each kind, Club themes and ×1.5 pass XP.","{0} por mês, renova mensalmente. Cancele quando quiser no Google Play > Assinaturas. Sem anúncios, 100 moedas diárias, 1 poder diário de cada tipo, temas do Clube e ×1,5 XP do passe.")
k("shop.coins","Monedas","Coins","Moedas")
k("shop.coins_amount","{0} monedas","{0} coins","{0} moedas")
k("shop.bonus","Incluye {0} % extra","Includes {0}% extra","Inclui {0}% a mais")
k("shop.boosters","Packs de potenciadores","Booster packs","Pacotes de poderes")
for key,es,en,pt,d1,d2,d3 in [("boosters_small","Pack de taller","Workshop pack","Pacote de oficina","2 Deshacer, 2 Girar, 1 Martillo","2 Undo, 2 Rotate, 1 Hammer","2 Desfazer, 2 Girar, 1 Martelo"),
 ("boosters_medium","Pack de artesano","Artisan pack","Pacote de artesão","4 Deshacer, 4 Girar, 3 Martillo, 2 Escoba, 2 Nueva bandeja","4 Undo, 4 Rotate, 3 Hammer, 2 Broom, 2 New tray","4 Desfazer, 4 Girar, 3 Martelo, 2 Vassoura, 2 Nova bandeja"),
 ("boosters_large","Pack de maestro","Master pack","Pacote de mestre","8 Deshacer, 8 Girar, 6 Martillo, 5 Escoba, 5 Nueva bandeja, 4 Reserva","8 Undo, 8 Rotate, 6 Hammer, 5 Broom, 5 New tray, 4 Hold","8 Desfazer, 8 Girar, 6 Martelo, 5 Vassoura, 5 Nova bandeja, 4 Reserva")]:
    k("shop."+key,es,en,pt); k("shop."+key+".desc",d1,d2,d3)
k("shop.coin_spend","Con monedas","With coins","Com moedas")
k("shop.protector","Protector de racha","Streak shield","Protetor de sequência")
k("shop.chest","Cofre del taller","Workshop chest","Baú da oficina")
k("shop.restore","Restaurar compras","Restore purchases","Restaurar compras")
k("shop.restore_ok","Listo: tus compras están al día.","Done: your purchases are up to date.","Pronto: suas compras estão em dia.")
k("shop.restore_fail","No se pudo conectar con Google Play. Inténtalo más tarde.","Couldn't reach Google Play. Try again later.","Não foi possível conectar ao Google Play. Tente mais tarde.")
k("shop.manage_subs","Gestionar suscripciones","Manage subscriptions","Gerenciar assinaturas")
for t,es,en,pt in [("theme_cobalto_antiguo","Tema Cobalto antiguo","Old Cobalt theme","Tema Cobalto antigo"),("theme_oliva_huerto","Tema Huerto de olivos","Olive grove theme","Tema Olival"),
 ("theme_ciruela_tarde","Tema Tarde ciruela","Plum evening theme","Tema Tarde ameixa"),("theme_turquesa_fuente","Tema Fuente turquesa","Turquoise fountain theme","Tema Fonte turquesa")]:
    k("theme."+t,es,en,pt)
# Cofre
k("chest.title","Cofre del taller","Workshop chest","Baú da oficina")
k("chest.odds","Probabilidades de cada premio:","Odds for each prize:","Probabilidade de cada prêmio:")
k("chest.open","Abrir por {0} monedas","Open for {0} coins","Abrir por {0} moedas")
k("chest.got","Te tocó: {0}","You got: {0}","Você ganhou: {0}")
for c,es,en,pt in [("coins_small","250 monedas","250 coins","250 moedas"),("coins_big","700 monedas","700 coins","700 moedas"),
 ("boosters_mix","Deshacer, Girar y Martillo","Undo, Rotate and Hammer","Desfazer, Girar e Martelo"),("protector","Protector de racha","Streak shield","Protetor de sequência"),
 ("tiles","40 azulejos","40 tiles","40 azulejos"),("cosmetic_trail","Estela de terracota","Terracotta trail","Rastro de terracota")]:
    k("chest."+c,es,en,pt)
# Pase
k("pass.info","Escalón {0} de {1} · quedan {2} días","Tier {0} of {1} · {2} days left","Nível {0} de {1} · faltam {2} dias")
k("pass.buy","Carril premium · {0}","Premium lane · {0}","Trilha premium · {0}")
k("pass.buy_plus","Pase Plus: premium + 10 escalones + sin anuncios esta temporada · {0}","Pass Plus: premium + 10 tiers + no ads this season · {0}","Passe Plus: premium + 10 níveis + sem anúncios nesta temporada · {0}")
k("pass.claimed","Cobrado","Claimed","Resgatado")
k("reward.coins","{0} monedas","{0} coins","{0} moedas"); k("reward.protectors","{0} protector(es)","{0} shield(s)","{0} protetor(es)")
k("reward.tiles","{0} azulejos","{0} tiles","{0} azulejos"); k("reward.cosmetic","Cosmético exclusivo","Exclusive cosmetic","Cosmético exclusivo")
# Ofertas
for o,t1,t2,t3,b1,b2,b3 in [
 ("starter_engaged","Para tu taller","For your workshop","Para sua oficina","Llevas varios días jugando: el Pack inicial trae 500 monedas, 5 potenciadores y un tema.","You've been playing for a few days: the Starter pack has 500 coins, 5 boosters and a theme.","Você joga há alguns dias: o Pacote inicial traz 500 moedas, 5 poderes e um tema."),
 ("pass_first_payer","Pase de temporada","Season pass","Passe da temporada","Recompensas dobles, tema y marco exclusivos durante 28 días.","Double rewards, exclusive theme and frame for 28 days.","Recompensas em dobro, tema e moldura exclusivos por 28 dias."),
 ("pass_repeat","Pase Plus","Pass Plus","Passe Plus","Premium, 10 escalones de salto y sin anuncios esta temporada.","Premium, 10 tier jump and no ads this season.","Premium, 10 níveis de salto e sem anúncios nesta temporada."),
 ("club_whale","Club Tesela","Tesela Club","Clube Tesela","Sin anuncios, monedas diarias y potenciadores cada día. Cancela cuando quieras.","No ads, daily coins and boosters every day. Cancel anytime.","Sem anúncios, moedas diárias e poderes todo dia. Cancele quando quiser."),
 ("gift_return","Qué bueno verte","Good to see you","Que bom te ver","Te guardamos un regalo de regreso: monedas y potenciadores, gratis.","We saved you a welcome-back gift: coins and boosters, free.","Guardamos um presente de volta: moedas e poderes, grátis.")]:
    k(f"offer.{o}.title",t1,t2,t3); k(f"offer.{o}.body",b1,b2,b3)
k("offer.claim_gift","Recoger regalo","Claim gift","Pegar presente")
# Ajustes
k("settings.audio","Sonido","Sound","Som"); k("settings.music","Música","Music","Música"); k("settings.sfx","Efectos","Effects","Efeitos")
k("settings.vibration","Vibración","Vibration","Vibração"); k("settings.access","Accesibilidad y control","Accessibility & controls","Acessibilidade e controle")
k("settings.reduce_motion","Reducir movimiento","Reduce motion","Reduzir movimento")
k("settings.strong_patterns","Patrones de color reforzados","Stronger color patterns","Padrões de cor reforçados")
k("settings.text_size","Tamaño de texto","Text size","Tamanho do texto"); k("settings.left_handed","Modo zurdo","Left-handed mode","Modo canhoto")
k("settings.lift","Elevación de la pieza","Piece lift","Elevação da peça")
k("settings.lift_cells","{0} celdas","{0} cells","{0} células")
k("settings.relaxed","Modo relajado (las 3 piezas siempre caben)","Relaxed mode (all 3 pieces always fit)","Modo relaxado (as 3 peças sempre cabem)")
k("settings.reminder","Recordatorio de sesión larga","Long session reminder","Lembrete de sessão longa")
k("settings.language","Idioma","Language","Idioma"); k("settings.notifications","Notificaciones","Notifications","Notificações")
k("settings.info","Información","Info","Informações"); k("settings.fair_play","Juego limpio","Fair play","Jogo limpo")
k("settings.credits","Créditos","Credits","Créditos"); k("settings.privacy","Privacidad","Privacy","Privacidade")
k("settings.privacy_policy","Política de privacidad","Privacy policy","Política de privacidade")
k("settings.delete_data","Borrar mis datos","Delete my data","Apagar meus dados")
k("lang.es","Español","Español","Español"); k("lang.en","English","English","English"); k("lang.pt","Português","Português","Português")
k("common.on","Sí","On","Sim"); k("common.off","No","Off","Não"); k("common.open","Abrir","Open","Abrir")
k("common.ok","Entendido","Got it","Entendi"); k("common.cancel","Cancelar","Cancel","Cancelar"); k("common.not_now","Ahora no","Not now","Agora não")
k("delete.body","Se borrará tu progreso de este dispositivo: monedas, niveles, ajustes y compras sin restaurar. Las compras se pueden recuperar con Restaurar compras.","This deletes your progress on this device: coins, levels, settings and unrestored purchases. Purchases can be recovered with Restore purchases.","Isto apaga seu progresso neste aparelho: moedas, níveis, ajustes e compras não restauradas. As compras podem ser recuperadas em Restaurar compras.")
k("delete.confirm","Borrar todo","Delete everything","Apagar tudo")
# Juego limpio
for i,(es,en,pt) in enumerate([
 ("Las piezas salen de un generador con semilla. La semilla aparece al final de cada partida.","Pieces come from a seeded generator. The seed is shown at the end of every game.","As peças vêm de um gerador com semente. A semente aparece no fim de cada partida."),
 ("El generador nunca sabe si viste anuncios, si compraste, tu país ni tu teléfono. Está separado del sistema de anuncios por diseño y un test lo comprueba.","The generator never knows whether you watched ads, bought anything, your country or your phone. It is separated from the ad system by design and a test checks it.","O gerador nunca sabe se você viu anúncios, comprou algo, seu país ou seu celular. Ele é separado do sistema de anúncios por projeto e um teste verifica isso."),
 ("Cada bandeja nueva trae al menos una pieza que cabe en tu patio.","Every new tray has at least one piece that fits your patio.","Cada bandeja nova traz pelo menos uma peça que cabe no seu pátio."),
 ("Nunca llegan más de dos piezas iguales seguidas.","Never more than two identical pieces in a row.","Nunca chegam mais de duas peças iguais seguidas."),
 ("La dificultad solo sube con tu puntuación, según la tabla pública de abajo, igual para todos.","Difficulty only rises with your score, following the public table below, the same for everyone.","A dificuldade só sobe com sua pontuação, conforme a tabela pública abaixo, igual para todos."),
 ("La partida termina solo cuando de verdad ninguna pieza cabe: se comprueba casilla por casilla.","A game ends only when truly no piece fits: every cell is checked.","A partida termina só quando de verdade nenhuma peça cabe: cada casa é verificada."),
 ("El reto diario usa la misma semilla para todo el mundo ese día.","The daily challenge uses the same seed for everyone that day.","O desafio diário usa a mesma semente para todos naquele dia.")],1):
    k(f"fair.rule{i}",es,en,pt)
k("fair.table","Peso de piezas grandes (5+ celdas) según la puntuación:","Weight of large pieces (5+ cells) by score:","Peso das peças grandes (5+ células) por pontuação:")
k("fair.table_row","desde {0} puntos: ×{1}","from {0} points: ×{1}","a partir de {0} pontos: ×{1}")
# Consentimiento
k("consent.title","Antes de seguir","Before we go on","Antes de continuar")
k("consent.body","Tesela se financia con compras opcionales y con anuncios en pausas naturales, nunca a mitad de partida. A continuación puede aparecer el formulario de privacidad de Google para elegir cómo se usan tus datos en los anuncios. Puedes cambiarlo cuando quieras en Ajustes > Privacidad.","Tesela is funded by optional purchases and by ads in natural breaks, never mid-game. Next, Google's privacy form may appear so you can choose how your data is used for ads. You can change it anytime in Settings > Privacy.","Tesela é financiado por compras opcionais e anúncios em pausas naturais, nunca no meio da partida. A seguir pode aparecer o formulário de privacidade do Google para você escolher como seus dados são usados nos anúncios. Você pode mudar quando quiser em Ajustes > Privacidade.")
k("consent.continue","Continuar","Continue","Continuar")
# Tutorial
k("tutorial.step1","Arrastra una pieza al patio","Drag a piece onto the patio","Arraste uma peça para o pátio")
k("tutorial.step2","Completa una fila o columna para limpiarla","Fill a row or column to clear it","Complete uma linha ou coluna para limpá-la")
k("tutorial.step3","Al colocar las tres piezas, llegan tres nuevas","Place all three and three new ones arrive","Coloque as três e chegam três novas")
# Recordatorio y notificaciones
k("reminder.title","Una pausa","Take a break","Uma pausa")
k("reminder.body","Llevas más de {0} minutos jugando. Tu partida queda guardada.","You've been playing for over {0} minutes. Your game is saved.","Você está jogando há mais de {0} minutos. Sua partida fica salva.")
k("notif.daily_ready","El reto diario de hoy ya está listo.","Today's daily challenge is ready.","O desafio diário de hoje está pronto.")
k("notif.streak_risk","Tu racha sigue viva: una partida hoy la mantiene.","Your streak is still alive: one game today keeps it.","Sua sequência continua: uma partida hoje a mantém.")

out = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Tesela", "Config", "Resources", "Loc")
os.makedirs(out, exist_ok=True)
for i, lang in enumerate(["es", "en", "pt"]):
    data = {"_comment": "Generado por tools/loc/build_loc.py — no editar a mano."}
    data.update({key: v[i] for key, v in sorted(T.items())})
    with open(os.path.join(out, lang + ".json"), "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=1)
print(len(T), "claves")
