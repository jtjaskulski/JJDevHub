import React from 'react';
import {ScrollView, StyleSheet, View} from 'react-native';
import {Card, Text, useTheme} from 'react-native-paper';

export function HomeScreen() {
  const theme = useTheme();

  return (
    <ScrollView
      style={[styles.container, {backgroundColor: theme.colors.background}]}
      contentContainerStyle={styles.content}>
      <View style={styles.hero}>
        <Text variant="headlineLarge" style={styles.title}>
          JJDevHub
        </Text>
        <Text variant="bodyLarge" style={styles.subtitle}>
          Personal developer portfolio & hub
        </Text>
      </View>

      <Card mode="outlined" style={styles.card}>
        <Card.Title
          title="Work Experience"
          subtitle="Career history & projects"
          left={props => (
            <Card.Cover
              {...props}
              source={require('react-native-vector-icons/MaterialIcons')}
            />
          )}
        />
        <Card.Content>
          <Text variant="bodyMedium">
            Browse through professional work experience, positions and project
            history.
          </Text>
        </Card.Content>
      </Card>

      <Card mode="outlined" style={styles.card}>
        <Card.Title
          title="Architecture"
          subtitle="Clean Architecture + DDD + CQRS"
        />
        <Card.Content>
          <Text variant="bodyMedium">
            Built with .NET 10, Angular, React Native, PostgreSQL, MongoDB,
            Kafka, Docker, and Jenkins.
          </Text>
        </Card.Content>
      </Card>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  content: {
    padding: 16,
    gap: 16,
  },
  hero: {
    alignItems: 'center',
    paddingVertical: 32,
  },
  title: {
    fontWeight: '300',
  },
  subtitle: {
    opacity: 0.7,
    marginTop: 4,
  },
  card: {
    marginBottom: 0,
  },
});
