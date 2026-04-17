import http from "k6/http";
import { check, sleep } from "k6";

// Usage: k6 run articles-pagination.js -e BASE_URL=http://localhost:5089
const baseUrl = __ENV.BASE_URL || "http://localhost:5239";

export const options = {
  stages: [
    { duration: "30s", target: 20 },
    { duration: "1m", target: 50 },
    { duration: "30s", target: 0 },
  ],
  thresholds: {
    http_req_failed: ["rate<0.05"],
    http_req_duration: ["p(95)<800"],
  },
};

export default function () {
  const page = (__VU * 17 + __ITER) % 200 + 1;
  const res = http.get(
    `${baseUrl}/api/articles?page=${page}&pageSize=50`,
    { tags: { name: "ArticlesList" } }
  );
  check(res, {
    "status 200": (r) => r.status === 200,
    "has items array": (r) => {
      try {
        const body = JSON.parse(r.body);
        return Array.isArray(body.items);
      } catch {
        return false;
      }
    },
  });
  sleep(0.05);
}
