# Task 4.4: React Native - Bazowa Mobilna Lista Postow (Opcjonalnie)

| Pole | Wartosc |
|------|---------|
| Sprint | 4 - Public Face & Mobile |
| Status | IN PROGRESS |
| Priorytet | Low |
| Estymacja | 8 story points |
| Powiazane pliki | `src/Clients/mobile/JJDevHubMobile/` |

## Opis

React Native 0.84 aplikacja mobilna sluzy jako natywny klient na iOS i Android. Prezentuje te same dane co Angular web (doswiadczenie zawodowe, blog posty) w natywnym UI z React Native Paper (Material Design 3). Jest to opcjonalny komponent - priorytet nizszy niz Angular web.

### Co juz jest zrobione

**Konfiguracja:**
- React Native 0.84 z React 19.2
- React Navigation (bottom-tabs, native)
- React Native Paper (MD3) z obsluга light/dark mode
- React Native Vector Icons (MaterialIcons)
- Node >= 22.11

**Nawigacja:**
- Bottom Tab Navigator z dwoma zakladkami: Home, Work Experience
- MaterialIcons dla ikon zakladek

**Ekrany:**
- `HomeScreen` - hero section, karty linki (Work Experience, Architecture)
- `WorkExperienceScreen` - FlatList z doswiadczeniami, loading indicator, error handling, formatowanie dat

**Serwisy:**
- `api.ts` - `getWorkExperiences(publicOnly)`: GET `/api/v1/content/work-experiences`
- Base URL: dev `http://10.0.2.2:8081` (Android emulator), prod `https://jjdevhub.com`

**Modele:**
- `WorkExperience` - id, companyName, position, startDate, endDate, isPublic, isCurrent, durationInMonths

**Platformy:**
- Android: Gradle + Kotlin (MainActivity.kt, MainApplication.kt)
- iOS: Swift AppDelegate, Podfile

### Co pozostalo

- Widok blog postow z infinite scroll
- Widok pojedynczego posta z syntax highlighting
- Integracja z Keycloak (deep linking, secure storage dla tokenow)
- Push notifications (Firebase Cloud Messaging)
- Offline mode z AsyncStorage cache
- Pull-to-refresh
- Animacje przejsc miedzy ekranami

## Kryteria akceptacji

- [x] React Native 0.84 z React 19.2
- [x] Bottom Tab Navigation (Home, Work Experience)
- [x] React Native Paper (MD3) z light/dark theme
- [x] HomeScreen z hero i kartami
- [x] WorkExperienceScreen z FlatList, loading, error states
- [x] API service z configurable base URL
- [ ] Blog posts screen z infinite scroll (FlatList pagination)
- [ ] Single post view z syntax highlighting
- [ ] Pull-to-refresh na listach
- [ ] Offline cache (AsyncStorage)
- [ ] Keycloak integration (opcjonalnie)

## Wymagane pakiety npm

| Pakiet | Wersja | Uzasadnienie |
|--------|--------|--------------|
| `react-native` | 0.84 | Juz zainstalowany - framework |
| `react-native-paper` | latest | Juz zainstalowany - Material Design 3 |
| `@react-navigation/bottom-tabs` | latest | Juz zainstalowany - tab navigation |
| `react-native-syntax-highlighter` | latest | NOWY - syntax highlighting w postach |
| `@react-native-async-storage/async-storage` | latest | NOWY - offline cache |
| `@react-native-firebase/messaging` | latest | NOWY - push notifications (opcjonalnie) |

## Architektura ekranow

```
App.tsx (NavigationContainer + PaperProvider)
└── Tab.Navigator (Bottom Tabs)
    ├── HomeScreen
    │   ├── Hero Card
    │   └── Feature Cards (Work Experience, Architecture)
    ├── WorkExperienceScreen
    │   ├── Loading (ActivityIndicator)
    │   ├── Error (Snackbar)
    │   └── FlatList<WorkExperience>
    │       └── Card (companyName, position, dates, duration)
    ├── BlogListScreen [TODO]
    │   └── FlatList<BlogPost> (infinite scroll)
    └── BlogPostScreen [TODO]
        └── ScrollView (markdown + syntax highlight)
```

## Zaleznosci

- **Wymaga:** Task 4.1 (Content API endpointy)
- **Blokuje:** Nic (opcjonalny komponent)

## Notatki techniczne

- Base URL `http://10.0.2.2:8081` jest specjalnym adresem Android emulatora ktory mapuje na `localhost` hosta. Na iOS uzyj `http://localhost:8081`.
- React Native Paper automatycznie dostosowuje theme (light/dark) do systemowych preferencji uzytkownika.
- `FlatList` z `onEndReached` i `onEndReachedThreshold` zapewnia infinite scroll bez ladowania wszystkich danych naraz.
- Keycloak integration w React Native wymaga `react-native-app-auth` (OAuth2) z deep linking (custom URL scheme) do obslugi redirect po logowaniu.
- React Native 0.84 uzywa New Architecture (Fabric, TurboModules) domyslnie - lepsze performance niz stara architektura.
