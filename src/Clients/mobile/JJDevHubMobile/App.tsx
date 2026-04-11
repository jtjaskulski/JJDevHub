import React from 'react';
import {StatusBar, useColorScheme} from 'react-native';
import {SafeAreaProvider} from 'react-native-safe-area-context';
import {
  MD3DarkTheme,
  MD3LightTheme,
  PaperProvider,
} from 'react-native-paper';
import {NavigationContainer} from '@react-navigation/native';
import {createBottomTabNavigator} from '@react-navigation/bottom-tabs';
import MaterialIcons from 'react-native-vector-icons/MaterialIcons';
import {HomeScreen} from './src/screens/HomeScreen';
import {WorkExperienceScreen} from './src/screens/WorkExperienceScreen';
import {BlogStackNavigator} from './src/navigation/BlogStack';

const Tab = createBottomTabNavigator();

function App() {
  const isDarkMode = useColorScheme() === 'dark';
  const theme = isDarkMode ? MD3DarkTheme : MD3LightTheme;

  return (
    <SafeAreaProvider>
      <PaperProvider theme={theme}>
        <StatusBar
          barStyle={isDarkMode ? 'light-content' : 'dark-content'}
          backgroundColor={theme.colors.surface}
        />
        <NavigationContainer>
          <Tab.Navigator
            screenOptions={({route}) => ({
              headerStyle: {backgroundColor: theme.colors.surface},
              headerTintColor: theme.colors.onSurface,
              tabBarStyle: {backgroundColor: theme.colors.surface},
              tabBarActiveTintColor: theme.colors.primary,
              tabBarInactiveTintColor: theme.colors.onSurfaceVariant,
              tabBarIcon: ({color, size}) => {
                const icons: Record<string, string> = {
                  Home: 'home',
                  'Work Experience': 'work',
                  Blog: 'article',
                };
                return (
                  <MaterialIcons
                    name={icons[route.name] || 'circle'}
                    size={size}
                    color={color}
                  />
                );
              },
            })}>
            <Tab.Screen name="Home" component={HomeScreen} />
            <Tab.Screen
              name="Work Experience"
              component={WorkExperienceScreen}
            />
            <Tab.Screen
              name="Blog"
              component={BlogStackNavigator}
              options={{headerShown: false}}
            />
          </Tab.Navigator>
        </NavigationContainer>
      </PaperProvider>
    </SafeAreaProvider>
  );
}

export default App;
