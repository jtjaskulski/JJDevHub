import React, {useCallback, useEffect, useState} from 'react';
import {FlatList, StyleSheet, View} from 'react-native';
import {
  ActivityIndicator,
  Card,
  Icon,
  Text,
  useTheme,
} from 'react-native-paper';
import {WorkExperience} from '../models/work-experience';
import {getWorkExperiences} from '../services/api';

function formatDate(dateStr: string): string {
  const date = new Date(dateStr);
  return date.toLocaleDateString('en-US', {year: 'numeric', month: 'short'});
}

export function WorkExperienceScreen() {
  const theme = useTheme();
  const [experiences, setExperiences] = useState<WorkExperience[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getWorkExperiences(true);
      setExperiences(data);
    } catch (err) {
      setError('Failed to load work experiences. Is the API running?');
      console.error('API error:', err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadData();
  }, [loadData]);

  if (loading) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator size="large" />
      </View>
    );
  }

  if (error) {
    return (
      <View style={[styles.centered, styles.errorContainer]}>
        <Icon source="alert-circle-outline" size={48} color={theme.colors.error} />
        <Text variant="bodyLarge" style={{color: theme.colors.error, marginTop: 12}}>
          {error}
        </Text>
      </View>
    );
  }

  return (
    <FlatList
      style={{backgroundColor: theme.colors.background}}
      contentContainerStyle={styles.list}
      data={experiences}
      keyExtractor={item => item.id}
      ListEmptyComponent={
        <Card mode="outlined">
          <Card.Content>
            <Text variant="bodyMedium">No work experiences found.</Text>
          </Card.Content>
        </Card>
      }
      renderItem={({item}) => (
        <Card mode="outlined" style={styles.card}>
          <Card.Title title={item.position} subtitle={item.companyName} />
          <Card.Content>
            <View style={styles.period}>
              <Icon source="calendar" size={16} />
              <Text variant="bodySmall" style={styles.periodText}>
                {formatDate(item.period.start)} —{' '}
                {item.period.end ? formatDate(item.period.end) : 'Present'}
              </Text>
            </View>
          </Card.Content>
        </Card>
      )}
    />
  );
}

const styles = StyleSheet.create({
  centered: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  errorContainer: {
    padding: 24,
  },
  list: {
    padding: 16,
    gap: 12,
  },
  card: {
    marginBottom: 0,
  },
  period: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    marginTop: 4,
    opacity: 0.7,
  },
  periodText: {
    fontSize: 13,
  },
});
