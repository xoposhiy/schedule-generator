﻿namespace Domain2;

/// <summary>
/// Место провидения пары
/// </summary>
/// <param name="Place">Место типа МатМех или Контур</param>
/// <param name="ClassRoom">Аудитория, если известна</param>
public record Location(string Place, string? ClassRoom = null);