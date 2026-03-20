import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { BlogService } from '../../services/blog.service';
import { BlogPost } from '../../models/blog-post.model';

@Component({
  selector: 'app-blog-list',
  imports: [RouterLink, MatCardModule, MatButtonModule, MatIconModule],
  templateUrl: './blog-list.page.html',
  styleUrl: './blog-list.page.scss',
})
export class BlogListPage implements OnInit {
  posts = signal<BlogPost[]>([]);

  constructor(private blog: BlogService) {}

  ngOnInit(): void {
    this.blog.getPosts().subscribe((list) => this.posts.set(list));
  }
}
