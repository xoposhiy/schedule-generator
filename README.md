# Составитель расписания - часть 1 (1-2 курсы)
Программа принимает на вход [3 таблицы](https://docs.google.com/spreadsheets/d/1JJm-ZBoHfumv82gRpGCdF_XZBPi_wpptUaHooGj3fBU) (учебный план, требования преподавателей, аудитории) и выдает сгенерированное расписание

## Общая схема
Распарсив требования, программа создает первичные meeting-и (пары). У них еще нет конкретного времени, аудитории и группы.
Далее по первичному meeting-у можно сгенерировать кучу вторичных с конкретными группами, местом и временем проведения. На каждом шаге алгоритма выбирается одна вторичная пара, которую надо реализовать (ну т.е. поставить в расписание).
Разные алгоритмы по разному выбирают какую вторичную пару реализовать (но везде так или иначе участвуют estimator-ы)
Эстиматоры - это маленькие классы - оценщики расписания. С их помощью определяется качество расписания (причем можно оценивать и не до конца составленное расписание)
Примеры эстиматоров: количество окон, количество пар в день (не должно слишком или слишком мало), количество смен локаций (студентам не хочется мотаться по всему городу)

Сейчас есть два актульных сценария составления расписания:

1. BeamSearch - компромис между полным перебором и жадным алгоритмом. Можете прочитать о нем в интернете. Оценка узла поиска происходит с доставлением пар жадным алгоритмом.
2. Repeater - в жадный алгоритм добавлен рандом. Этот жадный алгоритм составляет много расписаний, из которых выбирается лучший.

Результат поиска красиво оформляется в таблице.
Опционально может записать расписания в других форматах.

# Составитель расписания - часть 2 (3-4 курсы)
Coming soon
