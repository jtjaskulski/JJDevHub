import React, {useCallback, useEffect, useState} from 'react';
import {
  FlatList,
  RefreshControl,
  StyleSheet,
  View,
} from 'react-native';
import {Card, Text} from 'react-native-paper';
import AsyncStorage from '@react-native-async-storage/async-storage';
import type {NativeStackScreenProps} from '@react-navigation/native-stack';
import {MOCK_BLOG_POSTS, type BlogPost} from '../data/blogMock';
import type {BlogStackParamList} from '../navigation/BlogStack';

const CACHE_KEY = '@jjdevhub/blog_posts_cache';

type Props = NativeStackScreenProps<BlogStackParamList, 'BlogList'>;

export function BlogListScreen({navigation}: Props) {
  const [posts, setPosts] = useState<BlogPost[]>(MOCK_BLOG_POSTS);
  const [refreshing, setRefreshing] = useState(false);

  const loadFromCache = useCallback(async () => {
    try {
      const raw = await AsyncStorage.getItem(CACHE_KEY);
      if (raw) {
        const parsed = JSON.parse(raw) as BlogPost[];
        if (Array.isArray(parsed) && parsed.length > 0) {
          setPosts(parsed);
        }
      }
    } catch {
      /* ignore */
    }
  }, []);

  useEffect(() => {
    void loadFromCache();
  }, [loadFromCache]);

  const onRefresh = useCallback(async () => {
    setRefreshing(true);
    try {
      await AsyncStorage.setItem(CACHE_KEY, JSON.stringify(MOCK_BLOG_POSTS));
      setPosts([...MOCK_BLOG_POSTS]);
    } finally {
      setRefreshing(false);
    }
  }, []);

  return (
    <FlatList
      data={posts}
      keyExtractor={item => item.slug}
      refreshControl={
        <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
      }
      onEndReachedThreshold={0.4}
      contentContainerStyle={styles.list}
      renderItem={({item}) => (
        <Card
          style={styles.card}
          onPress={() => navigation.navigate('BlogPost', {post: item})}>
          <Card.Title title={item.title} subtitle={item.excerpt} />
        </Card>
      )}
      ListFooterComponent={<View style={styles.footer} />}
    />
  );
}

const styles = StyleSheet.create({
  list: {padding: 12},
  card: {marginBottom: 12},
  footer: {height: 24},
});
