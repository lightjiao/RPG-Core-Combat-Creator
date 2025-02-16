﻿using System;
using GameDevTV.Utils;
using UnityEngine;

namespace RPG.Stats
{
    public class BaseStats : MonoBehaviour
    {
        [Range(1, 99)]
        [SerializeField] private int startingLevel = 1;
        [SerializeField] private CharacterClass characterClass = CharacterClass.Grunt;
        [SerializeField] private Progression progression = null;
        [SerializeField] private GameObject levelUpParticleEffect = null;

        public event Action onLevelUp;

        LazyValue<int> currentLevel;
        Experience experience;

        private void Awake()
        {
            experience = GetComponent<Experience>();
            currentLevel = new LazyValue<int>(CalculateLevel);
        }

        private void OnEnable()
        {
            if (experience != null)
            {
                experience.onExperienceGained += UpdateLevel;
            }
        }

        private void Start()
        {
            currentLevel.ForceInit();
        }

        private void OnDisable()
        {
            if (experience != null)
            {
                experience.onExperienceGained -= UpdateLevel;
            }
        }

        private void UpdateLevel()
        {
            int newLevel = CalculateLevel();
            if (newLevel > currentLevel.value)
            {
                currentLevel.value = newLevel;

                LevelUpEffect();
                onLevelUp();
            }
        }

        private void LevelUpEffect()
        {
            Instantiate(levelUpParticleEffect, this.transform);
        }

        public float GetStat(Stat stat)
        {
            return (GetBaseStat(stat) + GetAddittiveModifier(stat)) * (1 + GetPercentageModifier(stat) / 100);
        }

        private float GetBaseStat(Stat stat)
        {
            return progression.GetStat(stat, characterClass, GetLevel());
        }

        public int GetLevel()
        {
            return currentLevel.value;
        }

        private float GetAddittiveModifier(Stat stat)
        {
            float total = 0;
            foreach (var provider in GetComponents<IModifierProvider>())
            {
                foreach (var modifiers in provider.GetAdditiveModifiers(stat))
                {
                    total += modifiers;
                }
            }

            return total;
        }

        private float GetPercentageModifier(Stat stat)
        {
            float total = 0;
            foreach (var provider in GetComponents<IModifierProvider>())
            {
                foreach (var modifiers in provider.GetPercentageModifiers(stat))
                {
                    total += modifiers;
                }
            }

            return total;
        }

        private int CalculateLevel()
        {
            Experience experience = GetComponent<Experience>();
            if (experience == null) return startingLevel;

            float currentExp = experience.ExperiencePoint;
            var penultimateLevel = progression.GetLevels(Stat.ExperienceToLevelUp, CharacterClass.Player);
            for (int level = 1; level < penultimateLevel; level++)
            {
                var expToLevelUp = progression.GetStat(Stat.ExperienceToLevelUp, CharacterClass.Player, level);
                if (expToLevelUp > currentExp)
                {
                    return level;
                }
            }

            return penultimateLevel + 1;
        }
    }
}