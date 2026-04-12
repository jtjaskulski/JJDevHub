import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Highlight } from 'ngx-highlightjs';
import { BlogService } from '../../services/blog.service';
import { BlogPost } from '../../models/blog-post.model';

@Component({
  selector: 'app-blog-post',
  imports: [RouterLink, MatButtonModule, MatIconModule, Highlight],
  templateUrl: './blog-post.page.html',
  styleUrl: './blog-post.page.scss',
})
export class BlogPostPage implements OnInit {
  post = signal<BlogPost | null>(null);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private blog: BlogService,
  ) {}

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug');
    if (!slug) {
      void this.router.navigate(['/blog']);
      return;
    }
    this.blog.getBySlug(slug).subscribe((p) => {
      if (!p) {
        void this.router.navigate(['/blog']);
        return;
      }
      this.post.set(p);
    });
  }
}
