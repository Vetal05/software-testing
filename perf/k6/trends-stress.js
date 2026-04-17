import http from "k6/http";
import { check, sleep } from "k6";

const baseUrl = __ENV.BASE_URL || "http://localhost:5239";

export const options = {
  scenarios: {
    stress_trends: {
      executor: "ramping-vus",
      startVUs: 0,
      stages: [
        { duration: "20s", target: 100 },
        { duration: "1m", target: 400 },
        { duration: "20s", target: 0 },
      ],
      gracefulRampDown: "10s",
    },
  },
  thresholds: {
    http_req_failed: ["rate<0.1"],
  },
};

export default function () {
  const res = http.get(`${baseUrl}/api/articles/trending?limit=25`, {
    tags: { name: "Trending" },
  });
  check(res, {
    "status 200": (r) => r.status === 200,
  });
  sleep(0.02);
}
