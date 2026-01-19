#!/usr/bin/env node

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ErrorCode,
  ListResourcesRequestSchema,
  ListToolsRequestSchema,
  McpError,
  ReadResourceRequestSchema,
} from '@modelcontextprotocol/sdk/types.js';

class HRQuestionnaireServer {
  constructor() {
    this.server = new Server(
      {
        name: 'hr-questionnaire-server',
        version: '1.0.0',
      },
      {
        capabilities: {
          resources: {},
          tools: {},
        },
      }
    );

    this.setupResourceHandlers();
    this.setupToolHandlers();
  }

  setupResourceHandlers() {
    this.server.setRequestHandler(ListResourcesRequestSchema, async () => ({
      resources: [
        {
          uri: 'question-bank://technical',
          name: 'Technical Questions',
          description: 'Standard technical interview questions for various roles',
          mimeType: 'application/json',
        },
        {
          uri: 'question-bank://behavioral',
          name: 'Behavioral Questions',
          description: 'Behavioral and situational interview questions',
          mimeType: 'application/json',
        },
        {
          uri: 'templates://senior-developer',
          name: 'Senior Developer Template',
          description: 'Pre-built questionnaire template for senior developer positions',
          mimeType: 'application/json',
        },
        {
          uri: 'templates://junior-developer',
          name: 'Junior Developer Template',
          description: 'Pre-built questionnaire template for junior developer positions',
          mimeType: 'application/json',
        },
        {
          uri: 'scoring-rubrics://programming',
          name: 'Programming Scoring Rubric',
          description: 'Standardized scoring criteria for programming questions',
          mimeType: 'application/json',
        },
        {
          uri: 'validation-rules://bias-check',
          name: 'Bias Validation Rules',
          description: 'Rules for detecting biased or inappropriate questions',
          mimeType: 'application/json',
        },
      ],
    }));

    this.server.setRequestHandler(ReadResourceRequestSchema, async (request) => {
      const { uri } = request.params;
      
      switch (uri) {
        case 'question-bank://technical':
          return {
            contents: [
              {
                uri,
                mimeType: 'application/json',
                text: JSON.stringify([
                  {
                    id: 'tech_001',
                    text: 'Describe your experience with version control systems.',
                    type: 'Text',
                    category: 'tools',
                    difficulty: 'intermediate',
                    suggestedOptions: [
                      { text: 'No experience', points: 0 },
                      { text: 'Basic Git commands', points: 3 },
                      { text: 'Advanced Git workflows', points: 7 },
                      { text: 'Git administration and best practices', points: 10 }
                    ]
                  },
                  {
                    id: 'tech_002',
                    text: 'How do you approach debugging complex issues?',
                    type: 'Text',
                    category: 'problem-solving',
                    difficulty: 'intermediate',
                    suggestedOptions: [
                      { text: 'Trial and error approach', points: 2 },
                      { text: 'Systematic debugging process', points: 6 },
                      { text: 'Advanced debugging tools and techniques', points: 10 }
                    ]
                  },
                  {
                    id: 'tech_003',
                    text: 'What testing methodologies do you follow?',
                    type: 'Choice',
                    category: 'quality',
                    difficulty: 'intermediate',
                    suggestedOptions: [
                      { text: 'No formal testing', points: 0 },
                      { text: 'Manual testing only', points: 3 },
                      { text: 'Unit testing', points: 6 },
                      { text: 'Comprehensive testing (unit, integration, E2E)', points: 10 }
                    ]
                  },
                  {
                    id: 'tech_004',
                    text: 'Rate your proficiency in database design.',
                    type: 'Rating',
                    category: 'database',
                    difficulty: 'intermediate',
                    suggestedOptions: [
                      { text: '1 - Beginner', points: 1 },
                      { text: '2 - Basic', points: 2 },
                      { text: '3 - Intermediate', points: 4 },
                      { text: '4 - Advanced', points: 7 },
                      { text: '5 - Expert', points: 10 }
                    ]
                  },
                  {
                    id: 'tech_005',
                    text: 'Describe a challenging technical problem you solved.',
                    type: 'Text',
                    category: 'experience',
                    difficulty: 'advanced',
                    suggestedOptions: [
                      { text: 'No specific examples', points: 0 },
                      { text: 'Simple problem description', points: 3 },
                      { text: 'Detailed problem with solution', points: 7 },
                      { text: 'Complex problem with innovative solution and impact', points: 10 }
                    ]
                  }
                ], null, 2)
              }
            ]
          };

        case 'question-bank://behavioral':
          return {
            contents: [
              {
                uri,
                mimeType: 'application/json',
                text: JSON.stringify([
                  {
                    id: 'behav_001',
                    text: 'Describe a situation where you had to work with a difficult team member.',
                    type: 'Text',
                    category: 'teamwork',
                    difficulty: 'intermediate',
                    suggestedOptions: [
                      { text: 'Avoided the situation', points: 0 },
                      { text: 'Confronted the person directly', points: 3 },
                      { text: 'Sought mediation or manager help', points: 6 },
                      { text: 'Successfully resolved through professional communication', points: 10 }
                    ]
                  },
                  {
                    id: 'behav_002',
                    text: 'How do you handle tight deadlines and pressure?',
                    type: 'Text',
                    category: 'stress-management',
                    difficulty: 'intermediate',
                    suggestedOptions: [
                      { text: 'Get overwhelmed and miss deadlines', points: 0 },
                      { text: 'Work extra hours but struggle', points: 4 },
                      { text: 'Prioritize effectively and communicate proactively', points: 8 },
                      { text: 'Excel under pressure with excellent time management', points: 10 }
                    ]
                  },
                  {
                    id: 'behav_003',
                    text: 'Tell me about a time you made a mistake and how you handled it.',
                    type: 'Text',
                    category: 'accountability',
                    difficulty: 'intermediate',
                    suggestedOptions: [
                      { text: 'Blame others or hide mistakes', points: 0 },
                      { text: 'Acknowledge but don\'t learn from it', points: 3 },
                      { text: 'Take responsibility and learn from it', points: 7 },
                      { text: 'Turn mistake into learning opportunity for team', points: 10 }
                    ]
                  }
                ], null, 2)
              }
            ]
          };

        case 'templates://senior-developer':
          return {
            contents: [
              {
                uri,
                mimeType: 'application/json',
                text: JSON.stringify({
                  name: 'Senior Developer Questionnaire',
                  description: 'Comprehensive questionnaire for senior developer positions',
                  questions: [
                    {
                      text: 'How many years of professional development experience do you have?',
                      type: 'Number',
                      required: true,
                      category: 'experience'
                    },
                    {
                      text: 'Describe your experience with system architecture and design.',
                      type: 'Text',
                      required: true,
                      category: 'architecture'
                    },
                    {
                      text: 'How do you mentor junior developers?',
                      type: 'Text',
                      required: true,
                      category: 'leadership'
                    },
                    {
                      text: 'What\'s your experience with code reviews and quality assurance?',
                      type: 'Choice',
                      required: true,
                      category: 'quality',
                      options: [
                        { text: 'No experience', points: 0 },
                        { text: 'Participate in reviews', points: 5 },
                        { text: 'Lead review process', points: 8 },
                        { text: 'Establish review standards and best practices', points: 10 }
                      ]
                    }
                  ],
                  scoringWeights: {
                    technical: 0.4,
                    behavioral: 0.3,
                    leadership: 0.2,
                    experience: 0.1
                  }
                }, null, 2)
              }
            ]
          };

        case 'templates://junior-developer':
          return {
            contents: [
              {
                uri,
                mimeType: 'application/json',
                text: JSON.stringify({
                  name: 'Junior Developer Questionnaire',
                  description: 'Entry-level questionnaire for junior developer positions',
                  questions: [
                    {
                      text: 'What programming languages are you proficient in?',
                      type: 'Text',
                      required: true,
                      category: 'technical'
                    },
                    {
                      text: 'Describe any personal or academic projects you\'ve worked on.',
                      type: 'Text',
                      required: true,
                      category: 'experience'
                    },
                    {
                      text: 'How do you approach learning new technologies?',
                      type: 'Text',
                      required: true,
                      category: 'learning'
                    },
                    {
                      text: 'Are you comfortable working in a team environment?',
                      type: 'Choice',
                      required: true,
                      category: 'teamwork',
                      options: [
                        { text: 'Prefer working alone', points: 2 },
                        { text: 'Comfortable with teamwork', points: 6 },
                        { text: 'Thrive in collaborative environments', points: 10 }
                      ]
                    }
                  ],
                  scoringWeights: {
                    technical: 0.5,
                    potential: 0.3,
                    teamwork: 0.2
                  }
                }, null, 2)
              }
            ]
          };

        case 'scoring-rubrics://programming':
          return {
            contents: [
              {
                uri,
                mimeType: 'application/json',
                text: JSON.stringify({
                  categories: {
                    'problem-solving': {
                      excellent: { points: 9-10, description: 'Innovative solutions, optimal algorithms' },
                      good: { points: 7-8, description: 'Effective solutions, good efficiency' },
                      average: { points: 5-6, description: 'Working solutions, basic efficiency' },
                      poor: { points: 0-4, description: 'Incomplete or inefficient solutions' }
                    },
                    'code-quality': {
                      excellent: { points: 9-10, description: 'Clean, maintainable, well-documented' },
                      good: { points: 7-8, description: 'Mostly clean, some documentation' },
                      average: { points: 5-6, description: 'Functional but needs improvement' },
                      poor: { points: 0-4, description: 'Messy, hard to maintain' }
                    },
                    'technical-knowledge': {
                      excellent: { points: 9-10, description: 'Deep understanding, best practices' },
                      good: { points: 7-8, description: 'Solid grasp of concepts' },
                      average: { points: 5-6, description: 'Basic understanding' },
                      poor: { points: 0-4, description: 'Limited knowledge' }
                    }
                  }
                }, null, 2)
              }
            ]
          };

        case 'validation-rules://bias-check':
          return {
            contents: [
              {
                uri,
                mimeType: 'application/json',
                text: JSON.stringify({
                  biasedTerms: [
                    'age', 'young', 'old', 'recent graduate', 'years of age',
                    'male', 'female', 'gender', 'married', 'single', 'kids',
                    'race', 'ethnicity', 'nationality', 'disability'
                  ],
                  redFlags: [
                    'questions about personal life',
                    'requirements unrelated to job performance',
                    'discriminatory language',
                    'cultural assumptions',
                    'physical requirements unless job-specific'
                  ],
                  recommendations: [
                    'Focus on skills and qualifications',
                    'Use inclusive language',
                    'Ensure job-relatedness',
                    'Avoid assumptions about background',
                    'Test for actual job requirements'
                  ]
                }, null, 2)
              }
            ]
          };

        default:
          throw new McpError(ErrorCode.InvalidParams, `Unknown resource: ${uri}`);
      }
    });
  }

  setupToolHandlers() {
    this.server.setRequestHandler(ListToolsRequestSchema, async () => ({
      tools: [
        {
          name: 'generate-questions',
          description: 'Generate relevant questions based on job description and requirements',
          inputSchema: {
            type: 'object',
            properties: {
              jobTitle: {
                type: 'string',
                description: 'Job title (e.g., Senior Software Engineer)',
              },
              jobDescription: {
                type: 'string',
                description: 'Full job description with requirements',
              },
              experience: {
                type: 'string',
                enum: ['entry', 'junior', 'mid', 'senior', 'lead'],
                description: 'Experience level',
              },
              questionTypes: {
                type: 'array',
                items: { type: 'string', enum: ['technical', 'behavioral', 'situational'] },
                description: 'Types of questions to generate',
              },
              count: {
                type: 'number',
                description: 'Number of questions to generate (default: 5)',
              },
            },
            required: ['jobTitle', 'jobDescription'],
          },
        },
        {
          name: 'validate-question',
          description: 'Validate questions for bias, clarity, and effectiveness',
          inputSchema: {
            type: 'object',
            properties: {
              question: {
                type: 'string',
                description: 'Question text to validate',
              },
              questionType: {
                type: 'string',
                description: 'Type of question (Text, Choice, Number, Rating)',
              },
              options: {
                type: 'array',
                items: {
                  type: 'object',
                  properties: {
                    text: { type: 'string' },
                    points: { type: 'number' }
                  }
                },
                description: 'Answer options (for choice questions)',
              },
            },
            required: ['question', 'questionType'],
          },
        },
        {
          name: 'suggest-points',
          description: 'Suggest optimal point values for question options',
          inputSchema: {
            type: 'object',
            properties: {
              question: {
                type: 'string',
                description: 'Question text',
              },
              questionType: {
                type: 'string',
                description: 'Type of question',
              },
              options: {
                type: 'array',
                items: { type: 'string' },
                description: 'Answer option texts',
              },
              difficulty: {
                type: 'string',
                enum: ['easy', 'intermediate', 'hard'],
                description: 'Question difficulty level',
              },
            },
            required: ['question', 'options'],
          },
        },
        {
          name: 'import-template',
          description: 'Import a pre-built questionnaire template',
          inputSchema: {
            type: 'object',
            properties: {
              templateType: {
                type: 'string',
                enum: ['senior-developer', 'junior-developer', 'team-lead', 'project-manager'],
                description: 'Type of template to import',
              },
              customize: {
                type: 'boolean',
                description: 'Allow customization of template (default: true)',
              },
            },
            required: ['templateType'],
          },
        },
        {
          name: 'analyze-performance',
          description: 'Analyze question performance and suggest improvements',
          inputSchema: {
            type: 'object',
            properties: {
              questionId: {
                type: 'string',
                description: 'Question identifier',
              },
              responseDistribution: {
                type: 'object',
                description: 'Distribution of responses (option -> count)',
              },
              averageScore: {
                type: 'number',
                description: 'Average score for this question',
              },
              totalResponses: {
                type: 'number',
                description: 'Total number of responses',
              },
            },
            required: ['questionId', 'responseDistribution'],
          },
        },
      ],
    }));

    this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
      const { name, arguments: args } = request.params;

      try {
        switch (name) {
          case 'generate-questions':
            return await this.generateQuestions(args);
          case 'validate-question':
            return await this.validateQuestion(args);
          case 'suggest-points':
            return await this.suggestPoints(args);
          case 'import-template':
            return await this.importTemplate(args);
          case 'analyze-performance':
            return await this.analyzePerformance(args);
          default:
            throw new McpError(ErrorCode.MethodNotFound, `Unknown tool: ${name}`);
        }
      } catch (error) {
        throw new McpError(ErrorCode.InternalError, `Tool execution failed: ${error.message}`);
      }
    });
  }

  async generateQuestions(args) {
    const { jobTitle, jobDescription, experience = 'mid', questionTypes = ['Text', 'Choice', 'Number', 'Rating'], count = 5 } = args;
    
    // Extract keywords from job description
    const keywords = this.extractKeywords(jobDescription);
    
    // Get relevant questions from question bank
    const questions = [];
    
    // Text questions
    if (questionTypes.includes('Text')) {
      const textQuestions = [
        {
          text: `Describe your experience with ${keywords.primaryTech || 'relevant technologies'}.`,
          type: 'Text',
          category: 'technical',
          suggestedOptions: []
        },
        {
          text: 'Describe a challenging situation you faced at work and how you resolved it.',
          type: 'Text',
          category: 'problem-solving',
          suggestedOptions: []
        },
        {
          text: 'How do you handle feedback and criticism in the workplace?',
          type: 'Text',
          category: 'professionalism',
          suggestedOptions: []
        },
        {
          text: `What interests you about the ${jobTitle || 'position'} role?`,
          type: 'Text',
          category: 'motivation',
          suggestedOptions: []
        },
        {
          text: 'Describe your approach to problem-solving when faced with unexpected challenges.',
          type: 'Text',
          category: 'analytical',
          suggestedOptions: []
        }
      ];
      const textCount = Math.ceil(count * 0.25); // 25% text questions
      questions.push(...textQuestions.slice(0, textCount));
    }
    
    // Choice questions
    if (questionTypes.includes('Choice')) {
      const choiceQuestions = [
        {
          text: 'How do you prefer to receive feedback on your work?',
          type: 'Choice',
          category: 'professionalism',
          options: [
            { text: 'I prefer not to receive feedback', points: 1 },
            { text: 'Written feedback via email', points: 4 },
            { text: 'One-on-one discussions', points: 7 },
            { text: 'Regular, constructive feedback in any format', points: 10 }
          ]
        },
        {
          text: 'What type of work environment helps you be most productive?',
          type: 'Choice',
          category: 'work-style',
          options: [
            { text: 'Quiet, isolated environment', points: 3 },
            { text: 'Collaborative team space', points: 6 },
            { text: 'Flexible hybrid arrangement', points: 8 },
            { text: 'Adaptable to various environments', points: 10 }
          ]
        },
        {
          text: 'How do you approach learning new technologies or skills?',
          type: 'Choice',
          category: 'learning',
          options: [
            { text: 'I wait for training to be provided', points: 2 },
            { text: 'I learn when required for projects', points: 5 },
            { text: 'I proactively explore new technologies', points: 8 },
            { text: 'I continuously learn and share knowledge with others', points: 10 }
          ]
        },
        {
          text: 'What motivates you most in a job role?',
          type: 'Choice',
          category: 'motivation',
          options: [
            { text: 'Job security and stability', points: 4 },
            { text: 'Competitive salary and benefits', points: 6 },
            { text: 'Career growth opportunities', points: 8 },
            { text: 'Meaningful work and impact', points: 10 }
          ]
        },
        {
          text: 'How do you handle conflicting priorities or deadlines?',
          type: 'Choice',
          category: 'time-management',
          options: [
            { text: 'I focus on one task at a time', points: 3 },
            { text: 'I seek guidance from my manager', points: 5 },
            { text: 'I prioritize based on urgency and importance', points: 8 },
            { text: 'I negotiate priorities and communicate proactively', points: 10 }
          ]
        }
      ];
      const choiceCount = Math.ceil(count * 0.25); // 25% choice questions
      questions.push(...choiceQuestions.slice(0, choiceCount));
    }
    
    // Number questions
    if (questionTypes.includes('Number')) {
      const numberQuestions = [
        {
          text: 'How many years of professional experience do you have?',
          type: 'Number',
          category: 'experience',
          options: [
            { text: '0-1 years (Entry level)', points: 2 },
            { text: '2-4 years (Junior)', points: 5 },
            { text: '5-7 years (Mid-level)', points: 8 },
            { text: '8+ years (Senior/Lead)', points: 10 }
          ]
        },
        {
          text: 'How many team projects have you collaborated on?',
          type: 'Number',
          category: 'teamwork',
          options: [
            { text: '0-2 projects', points: 2 },
            { text: '3-5 projects', points: 5 },
            { text: '6-10 projects', points: 8 },
            { text: '11+ projects', points: 10 }
          ]
        },
        {
          text: 'How many technical skills or programming languages are you proficient in?',
          type: 'Number',
          category: 'technical',
          options: [
            { text: '1-2 skills', points: 3 },
            { text: '3-5 skills', points: 6 },
            { text: '6-8 skills', points: 8 },
            { text: '9+ skills', points: 10 }
          ]
        }
      ];
      const numberCount = Math.ceil(count * 0.25); // 25% number questions
      questions.push(...numberQuestions.slice(0, numberCount));
    }
    
    // Rating questions
    if (questionTypes.includes('Rating')) {
      const ratingQuestions = [
        {
          text: 'Rate your proficiency with problem-solving and analytical thinking.',
          type: 'Rating',
          category: 'analytical',
          options: [
            { text: '1 (Beginner - Needs guidance)', points: 2 },
            { text: '2 (Basic - Can handle simple problems)', points: 4 },
            { text: '3 (Intermediate - Can solve most problems)', points: 6 },
            { text: '4 (Advanced - Handles complex issues)', points: 8 },
            { text: '5 (Expert - Innovative solutions)', points: 10 }
          ]
        },
        {
          text: 'Rate your ability to work effectively in a team environment.',
          type: 'Rating',
          category: 'teamwork',
          options: [
            { text: '1 (Prefer working alone)', points: 2 },
            { text: '2 (Comfortable in teams)', points: 4 },
            { text: '3 (Good team contributor)', points: 6 },
            { text: '4 (Strong collaborator)', points: 8 },
            { text: '5 (Exceptional team player)', points: 10 }
          ]
        },
        {
          text: 'Rate your written and verbal communication skills.',
          type: 'Rating',
          category: 'communication',
          options: [
            { text: '1 (Needs improvement)', points: 2 },
            { text: '2 (Basic communication)', points: 4 },
            { text: '3 (Clear and effective)', points: 6 },
            { text: '4 (Professional and polished)', points: 8 },
            { text: '5 (Outstanding communicator)', points: 10 }
          ]
        },
        {
          text: 'Rate your ability to adapt to new challenges and changes.',
          type: 'Rating',
          category: 'adaptability',
          options: [
            { text: '1 (Resistant to change)', points: 2 },
            { text: '2 (Slow to adapt)', points: 4 },
            { text: '3 (Adaptable with time)', points: 6 },
            { text: '4 (Quickly adapts)', points: 8 },
            { text: '5 (Thrives on change)', points: 10 }
          ]
        }
      ];
      const ratingCount = Math.ceil(count * 0.25); // 25% rating questions
      questions.push(...ratingQuestions.slice(0, ratingCount));
    }
    
    return {
      content: [
        {
          type: 'text',
          text: JSON.stringify({
            success: true,
            questions: questions.slice(0, count),
            metadata: {
              jobTitle,
              experience,
              keywords,
              generatedAt: new Date().toISOString()
            }
          }, null, 2)
        }
      ]
    };
  }

  extractKeywords(jobDescription) {
    // Simple keyword extraction
    const techKeywords = ['javascript', 'python', 'java', 'c#', 'sql', 'react', 'angular', 'node.js', 'aws', 'azure', 'docker'];
    const words = jobDescription.toLowerCase().split(/\s+/);
    
    const found = words.filter(word => 
      techKeywords.some(tech => word.includes(tech))
    );
    
    return {
      primaryTech: found[0] || 'technologies',
      secondaryTech: found[1] || 'systems',
      allTech: found
    };
  }

  async validateQuestion(args) {
    const { question, questionType, options = [] } = args;
    
    const validation = {
      isValid: true,
      warnings: [],
      suggestions: [],
      biasDetected: false,
      clarityScore: 0
    };
    
    // Check for biased terms
    const biasedTerms = ['age', 'young', 'old', 'male', 'female', 'married', 'kids'];
    const lowerQuestion = question.toLowerCase();
    
    for (const term of biasedTerms) {
      if (lowerQuestion.includes(term)) {
        validation.biasDetected = true;
        validation.warnings.push(`Potentially biased term detected: "${term}"`);
        validation.isValid = false;
      }
    }
    
    // Check question clarity
    if (question.length < 10) {
      validation.warnings.push('Question seems too short');
      validation.clarityScore -= 2;
    }
    
    if (!question.includes('?')) {
      validation.suggestions.push('Consider phrasing as a question');
      validation.clarityScore -= 1;
    }
    
    // Validate options for choice questions
    if (questionType === 'Choice') {
      if (options.length < 2) {
        validation.warnings.push('Choice questions need at least 2 options');
        validation.isValid = false;
      }
      
      const pointValues = options.map(opt => opt.points || 0);
      const hasValidRange = Math.max(...pointValues) > Math.min(...pointValues);
      if (!hasValidRange) {
        validation.suggestions.push('Consider using different point values for options');
      }
    }
    
    validation.clarityScore = Math.max(0, validation.clarityScore + 8);
    
    return {
      content: [
        {
          type: 'text',
          text: JSON.stringify({
            success: true,
            validation,
            recommendations: validation.isValid ? 
              ['Question looks good!'] : 
              ['Please address the warnings above']
          }, null, 2)
        }
      ]
    };
  }

  async suggestPoints(args) {
    const { question, options, difficulty = 'intermediate' } = args;
    
    const basePoints = {
      easy: { min: 1, max: 5 },
      intermediate: { min: 2, max: 8 },
      hard: { min: 5, max: 10 }
    };
    
    const range = basePoints[difficulty];
    const suggestions = [];
    
    // Analyze option quality and assign points
    options.forEach((option, index) => {
      let points = 0;
      
      // Simple heuristic: better answers get higher points
      if (option.toLowerCase().includes('expert') || option.toLowerCase().includes('advanced')) {
        points = range.max;
      } else if (option.toLowerCase().includes('proficient') || option.toLowerCase().includes('good')) {
        points = Math.floor(range.max * 0.8);
      } else if (option.toLowerCase().includes('basic') || option.toLowerCase().includes('some')) {
        points = Math.floor(range.max * 0.5);
      } else if (option.toLowerCase().includes('no') || option.toLowerCase().includes('none')) {
        points = range.min;
      } else {
        // Distribute remaining points
        points = Math.floor(range.min + (range.max - range.min) * (index / (options.length - 1)));
      }
      
      suggestions.push({
        option,
        suggestedPoints: points,
        reasoning: this.getPointReasoning(option, points)
      });
    });
    
    return {
      content: [
        {
          type: 'text',
          text: JSON.stringify({
            success: true,
            difficulty,
            suggestions,
            totalPoints: suggestions.reduce((sum, s) => sum + s.suggestedPoints, 0)
          }, null, 2)
        }
      ]
    };
  }

  async importTemplate(args) {
    const { templateType, customize = true } = args;
    
    // This would normally fetch from the template resources
    const templates = {
      'senior-developer': {
        name: 'Senior Developer Template',
        questions: [
          {
            text: 'Describe your experience with system architecture.',
            type: 'Text',
            category: 'architecture'
          },
          {
            text: 'How do you mentor junior developers?',
            type: 'Text',
            category: 'leadership'
          }
        ]
      },
      'junior-developer': {
        name: 'Junior Developer Template',
        questions: [
          {
            text: 'What programming languages are you proficient in?',
            type: 'Text',
            category: 'technical'
          },
          {
            text: 'Describe your learning approach for new technologies.',
            type: 'Text',
            category: 'learning'
          }
        ]
      }
    };
    
    const template = templates[templateType];
    if (!template) {
      throw new McpError(ErrorCode.InvalidParams, `Unknown template type: ${templateType}`);
    }
    
    return {
      content: [
        {
          type: 'text',
          text: JSON.stringify({
            success: true,
            template,
            customizable: customize,
            importedAt: new Date().toISOString()
          }, null, 2)
        }
      ]
    };
  }

  async analyzePerformance(args) {
    const { questionId, responseDistribution, averageScore, totalResponses } = args;
    
    const analysis = {
      questionId,
      totalResponses,
      averageScore,
      distribution: responseDistribution,
      insights: [],
      recommendations: []
    };
    
    // Analyze distribution
    const responses = Object.values(responseDistribution);
    const maxResponses = Math.max(...responses);
    const minResponses = Math.min(...responses);
    
    if (maxResponses > totalResponses * 0.8) {
      analysis.insights.push('One option is overwhelmingly selected - question may be too easy or unclear');
      analysis.recommendations.push('Consider rephrasing the question or adjusting options');
    }
    
    if (averageScore < 3) {
      analysis.insights.push('Low average score - question may be too difficult');
      analysis.recommendations.push('Consider providing more guidance or simplifying the question');
    } else if (averageScore > 8) {
      analysis.insights.push('High average score - question may be too easy');
      analysis.recommendations.push('Consider making the question more challenging');
    }
    
    if (totalResponses < 10) {
      analysis.insights.push('Limited response data - collect more responses for better analysis');
    }
    
    return {
      content: [
        {
          type: 'text',
          text: JSON.stringify({
            success: true,
            analysis,
            analyzedAt: new Date().toISOString()
          }, null, 2)
        }
      ]
    };
  }

  extractKeywords(text) {
    // Simple keyword extraction
    const techKeywords = ['javascript', 'python', 'java', 'c#', 'react', 'angular', 'node', 'sql', 'aws', 'azure'];
    const found = techKeywords.filter(keyword => text.toLowerCase().includes(keyword));
    
    return {
      primaryTech: found[0] || null,
      allTech: found,
      experience: text.toLowerCase().includes('senior') ? 'senior' : 
                 text.toLowerCase().includes('junior') ? 'junior' : 'mid'
    };
  }

  getPointReasoning(option, points) {
    if (points >= 8) return 'Excellent answer - demonstrates expertise';
    if (points >= 6) return 'Good answer - shows competence';
    if (points >= 4) return 'Acceptable answer - basic understanding';
    return 'Limited answer - needs improvement';
  }

  async run() {
    const transport = new StdioServerTransport();
    await this.server.connect(transport);
    console.error('HR Questionnaire MCP server running on stdio');
  }
}

const server = new HRQuestionnaireServer();
server.run().catch(console.error);
